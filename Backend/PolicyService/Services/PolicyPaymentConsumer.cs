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
            await channel.QueueBindAsync(_options.PolicyPaymentsQueueName, _options.ExchangeName, "PaymentFailed", cancellationToken: stoppingToken);

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

                    switch (args.RoutingKey)
                    {
                        case "PaymentCompleted":
                            var payment = JsonSerializer.Deserialize<PaymentCompletedMessage>(dataElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (payment?.CustomerPolicyId is null)
                            {
                                await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: CancellationToken.None);
                                return;
                            }

                            await HandlePaymentCompletedAsync(payment, repository, publisher, stoppingToken);
                            break;

                        case "PaymentFailed":
                            var failedPayment = JsonSerializer.Deserialize<PaymentFailedMessage>(dataElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (failedPayment?.CustomerPolicyId is null)
                            {
                                await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: CancellationToken.None);
                                return;
                            }

                            await HandlePaymentFailedAsync(failedPayment, repository, publisher, stoppingToken);
                            break;
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

        private static async Task HandlePaymentCompletedAsync(PaymentCompletedMessage payment, IPolicyRepository repository, IEventPublisher publisher, CancellationToken cancellationToken)
        {
            var customerPolicy = await repository.GetCustomerPolicyAsync(payment.CustomerPolicyId!.Value, cancellationToken);
            if (customerPolicy is null || customerPolicy.PendingOperation is null)
            {
                return;
            }

            if (customerPolicy.PendingOperation == PolicyPaymentOperation.Purchase)
            {
                customerPolicy.Status = CustomerPolicyStatus.Active;
                customerPolicy.PendingOperation = null;
                customerPolicy.LastPaymentFailureReason = null;
                customerPolicy.LastPaymentFailedOnUtc = null;
                await repository.SaveChangesAsync(cancellationToken);
                await publisher.PublishAsync("PolicyPurchased", new { customerPolicy.CustomerPolicyId, customerPolicy.PolicyId, customerPolicy.CustomerId, customerPolicy.VehicleNumber, customerPolicy.DrivingLicenseNumber, payment.PaymentId }, cancellationToken);
            }
            else if (customerPolicy.PendingOperation == PolicyPaymentOperation.Renewal)
            {
                customerPolicy.EndDate = customerPolicy.EndDate.AddYears(1);
                customerPolicy.Status = CustomerPolicyStatus.Renewed;
                customerPolicy.PendingOperation = null;
                customerPolicy.LastPaymentFailureReason = null;
                customerPolicy.LastPaymentFailedOnUtc = null;
                await repository.SaveChangesAsync(cancellationToken);
                await publisher.PublishAsync("PolicyRenewed", new { customerPolicy.CustomerPolicyId, customerPolicy.PolicyId, customerPolicy.CustomerId, payment.PaymentId }, cancellationToken);
            }
        }

        private static async Task HandlePaymentFailedAsync(PaymentFailedMessage payment, IPolicyRepository repository, IEventPublisher publisher, CancellationToken cancellationToken)
        {
            var customerPolicy = await repository.GetCustomerPolicyAsync(payment.CustomerPolicyId!.Value, cancellationToken);
            if (customerPolicy is null || customerPolicy.PendingOperation is null)
            {
                return;
            }

            customerPolicy.LastPaymentFailureReason = payment.Reason;
            customerPolicy.LastPaymentFailedOnUtc = DateTime.UtcNow;

            if (customerPolicy.PendingOperation == PolicyPaymentOperation.Purchase)
            {
                customerPolicy.Status = CustomerPolicyStatus.Cancelled;
            }
            else if (customerPolicy.PendingOperation == PolicyPaymentOperation.Renewal)
            {
                customerPolicy.Status = customerPolicy.EndDate < DateTime.UtcNow.Date
                    ? CustomerPolicyStatus.Expired
                    : CustomerPolicyStatus.Active;
            }

            customerPolicy.PendingOperation = null;
            await repository.SaveChangesAsync(cancellationToken);
            await publisher.PublishAsync("PolicyPaymentFailed", new
            {
                customerPolicy.CustomerPolicyId,
                customerPolicy.PolicyId,
                customerPolicy.CustomerId,
                payment.PaymentId,
                payment.Operation,
                payment.Reason,
                customerPolicy.Status
            }, cancellationToken);
        }
    }
}
