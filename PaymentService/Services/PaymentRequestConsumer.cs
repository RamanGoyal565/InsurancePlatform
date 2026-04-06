using Microsoft.Extensions.Options;
using PaymentService.Config;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace PaymentService.Services
{
    public sealed class PaymentRequestConsumer(IServiceScopeFactory scopeFactory, IOptions<RabbitMqOptions> options) : BackgroundService
    {
        private readonly RabbitMqOptions _options = options.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory { HostName = _options.HostName, UserName = _options.UserName, Password = _options.Password };
            await using var connection = await factory.CreateConnectionAsync(stoppingToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.ExchangeDeclareAsync(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: stoppingToken);
            await channel.QueueDeclareAsync(_options.PaymentRequestsQueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            await channel.QueueBindAsync(_options.PaymentRequestsQueueName, _options.ExchangeName, "PaymentRequested", cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, args) =>
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                    using var envelope = JsonDocument.Parse(Encoding.UTF8.GetString(args.Body.ToArray()));
                    if (!envelope.RootElement.TryGetProperty("Data", out var dataElement))
                    {
                        await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: CancellationToken.None);
                        return;
                    }

                    var request = JsonSerializer.Deserialize<PolicyPaymentRequest>(dataElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (request is null)
                    {
                        await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: CancellationToken.None);
                        return;
                    }

                    await paymentService.ProcessPolicyPaymentAsync(request, stoppingToken);
                    await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: CancellationToken.None);
                }
                catch
                {
                    await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None);
                }
            };

            await channel.BasicConsumeAsync(_options.PaymentRequestsQueueName, autoAck: false, consumer, cancellationToken: stoppingToken);

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
