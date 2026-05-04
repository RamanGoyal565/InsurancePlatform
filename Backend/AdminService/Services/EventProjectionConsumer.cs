using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AdminService.DTOs;
using AdminService.Models;
using AdminService.Repositories;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using AdminService.Config;

namespace AdminService.Services;
public sealed record CurrentUser(Guid UserId, string Role);

public sealed class EventProjectionConsumer(IServiceScopeFactory scopeFactory, IOptions<RabbitMqOptions> options) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = _options.HostName, UserName = _options.UserName, Password = _options.Password };
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(_options.QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(_options.QueueName, _options.ExchangeName, "#", cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IAdminReadRepository>();
                var payload = Encoding.UTF8.GetString(args.Body.ToArray());
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;
                var audit = new EventAudit
                {
                    EventType = root.GetProperty("EventType").GetString() ?? "UnknownEvent",
                    Payload = root.GetProperty("Data").ToString(),
                    OccurredOnUtc = root.TryGetProperty("OccurredOnUtc", out var occurred) && occurred.ValueKind == JsonValueKind.String && DateTime.TryParse(occurred.GetString(), out var parsed)
                        ? parsed
                        : DateTime.UtcNow
                };
                await repository.AddAsync(audit, stoppingToken);
                await repository.SaveChangesAsync(stoppingToken);
                await channel.BasicAckAsync(args.DeliveryTag, false, cancellationToken: CancellationToken.None);
            }
            catch
            {
                await channel.BasicNackAsync(args.DeliveryTag, false, true, cancellationToken: CancellationToken.None);
            }
        };

        await channel.BasicConsumeAsync(_options.QueueName, autoAck: false, consumer, cancellationToken: stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
