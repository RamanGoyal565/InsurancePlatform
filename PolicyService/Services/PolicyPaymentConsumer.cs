using Microsoft.Extensions.Options;
using PolicyService.Config;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using PolicyService.Repositories;
using System.Text.Json;
using System.Text;
using PolicyService.Models;

namespace PolicyService.Services
{
    public sealed class PolicyPaymentConsumer(IServiceScopeFactory scopeFactory, IOptions<RabbitMqOptions> options) : BackgroundService
    {
        private readonly RabbitMqOptions _options = options.Value;
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory { HostName = _options.HostName, UserName = _options.UserName, Password = _options.Password };
            await using var connection = await factory.CreateConnectionAsync(stoppingToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.ExchangeDeclareAsync(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: stoppingToken);
            await channel.QueueDeclareAsync(_options.PolicyPaymentsQueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            await channel.QueueBindAsync(_options.PolicyPaymentsQueueName, _options.ExchangeName, "PaymentCompleted", cancellationToken: stoppingToken);
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, args) =>
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IPolicyRepository>();
                    var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
                    using var envelope = JsonDocument.Parse(Encoding.UTF8.GetString(args.Body.ToArray()));
                    if (!envelope.RootElement.TryGetProperty("Data", out var dataElement))
                    {
                        await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: CancellationToken.None);
                        return;
                    }

                    var payment = JsonSerializer.Deserialize<PaymentCompletedMessage>(dataElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (payment?.CustomerPolicyId is null)
                    {
                        await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: CancellationToken.None);
                        return;
                    }
                    var customerPolicy = await repository.GetCustomerPolicyAsync(payment.CustomerPolicyId.Value, stoppingToken);
                    if (customerPolicy is null || customerPolicy.PendingOperation is null)
                    {
                        await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: CancellationToken.None);
                        return;
                    }
                    if (customerPolicy.PendingOperation == PolicyPaymentOperation.Purchase)
                    {
                        customerPolicy.Status = CustomerPolicyStatus.Active;
                        customerPolicy.PendingOperation = null;
                        await repository.SaveChangesAsync(stoppingToken);
                        await publisher.PublishAsync("PolicyPurchased", new { customerPolicy.CustomerPolicyId, customerPolicy.PolicyId, customerPolicy.CustomerId, customerPolicy.VehicleNumber, customerPolicy.DrivingLicenseNumber, payment.PaymentId }, stoppingToken);
                    }
                    else if (customerPolicy.PendingOperation == PolicyPaymentOperation.Renewal)
                    {
                        customerPolicy.EndDate = customerPolicy.EndDate.AddYears(1);
                        customerPolicy.Status = CustomerPolicyStatus.Renewed;
                        customerPolicy.PendingOperation = null;
                        await repository.SaveChangesAsync(stoppingToken);
                        await publisher.PublishAsync("PolicyRenewed", new { customerPolicy.CustomerPolicyId, customerPolicy.PolicyId, customerPolicy.CustomerId, payment.PaymentId }, stoppingToken);
                    }
                    await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: CancellationToken.None);
                }
                catch
                {
                    await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None);
                }
            };
            await channel.BasicConsumeAsync(_options.PolicyPaymentsQueueName, autoAck: false, consumer, cancellationToken: stoppingToken);
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }
    }
}