using IdentityService.Config;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace IdentityService.Services
{
    public sealed class RabbitMqEventPublisher(IOptions<RabbitMqOptions> options) : IEventPublisher
    {
        private readonly RabbitMqOptions _options = options.Value;

        public async Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory { HostName = _options.HostName, UserName = _options.UserName, Password = _options.Password };
            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await channel.ExchangeDeclareAsync(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: cancellationToken);

            var envelope = new { EventType = eventType, OccurredOnUtc = DateTime.UtcNow, Data = payload };
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));
            var properties = new BasicProperties { Persistent = true };

            await channel.BasicPublishAsync(_options.ExchangeName, eventType, false, properties, body, cancellationToken);
        }
    }
}
