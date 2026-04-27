using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NotificationService.Config;
using NotificationService.Models;
using NotificationService.Repositories;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Services
{
    public sealed class NotificationConsumer(IServiceScopeFactory scopeFactory, IOptions<RabbitMqOptions> options) : BackgroundService
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
                    var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                    var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                    using var document = JsonDocument.Parse(Encoding.UTF8.GetString(args.Body.ToArray()));
                    var root = document.RootElement;
                    var eventType = root.GetProperty("EventType").GetString() ?? "UnknownEvent";
                    var data = root.TryGetProperty("Data", out var dataElement) ? dataElement : default;

                    if (eventType == "UserRegistered")
                    {
                        var userId = TryReadGuid(data, "UserId");
                        var email = TryReadString(data, "Email");
                        var name = TryReadString(data, "Name");
                        if (userId.HasValue && !string.IsNullOrWhiteSpace(email))
                        {
                            await repository.UpsertUserContactAsync(userId.Value, email, name, stoppingToken);
                        }
                    }

                    var recipients = GetRecipients(data);
                    var message = BuildMessage(eventType, data);
                    var subject = BuildSubject(eventType);

                    if (recipients.Count == 0)
                    {
                        await repository.AddAsync(new Notification { UserId = null, Message = message }, stoppingToken);
                    }
                    else
                    {
                        foreach (var recipient in recipients)
                        {
                            await repository.AddAsync(new Notification { UserId = recipient, Message = message }, stoppingToken);
                        }
                    }

                    await repository.SaveChangesAsync(stoppingToken);

                    if (recipients.Count > 0)
                    {
                        var contacts = await repository.GetUserContactsAsync(recipients, stoppingToken);
                        foreach (var contact in contacts)
                        {
                            await emailSender.SendAsync(contact.Email, subject, message, stoppingToken);
                        }
                    }

                    await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: CancellationToken.None);
                }
                catch
                {
                    await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None);
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

        private static HashSet<Guid> GetRecipients(JsonElement data)
        {
            var recipients = new HashSet<Guid>();
            AddRecipient(recipients, data, "UserId");
            AddRecipient(recipients, data, "CustomerId");
            AddRecipient(recipients, data, "AssignedTo");
            AddRecipient(recipients, data, "ChangedBy");
            AddRecipient(recipients, data, "CommentedBy");
            return recipients;
        }

        private static void AddRecipient(ISet<Guid> recipients, JsonElement data, string propertyName)
        {
            if (data.ValueKind != JsonValueKind.Object || !data.TryGetProperty(propertyName, out var value)) return;
            if (value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var parsed)) recipients.Add(parsed);
        }

        private static Guid? TryReadGuid(JsonElement data, string propertyName)
        {
            if (data.ValueKind != JsonValueKind.Object || !data.TryGetProperty(propertyName, out var value)) return null;
            return value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var parsed) ? parsed : null;
        }

        private static string? TryReadString(JsonElement data, string propertyName)
        {
            if (data.ValueKind != JsonValueKind.Object || !data.TryGetProperty(propertyName, out var value)) return null;
            return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }

        private static string BuildSubject(string eventType) => eventType switch
        {
            "TicketUpdated" => "Insurance Platform: Ticket status updated",
            "TicketAssigned" => "Insurance Platform: Ticket assigned",
            "CommentAdded" => "Insurance Platform: New ticket comment",
            "ClaimApproved" => "Insurance Platform: Claim approved",
            "ClaimRejected" => "Insurance Platform: Claim rejected",
            "PolicyPurchased" => "Insurance Platform: Policy purchased",
            "PolicyRenewed" => "Insurance Platform: Policy renewed",
            "PolicyExpired" => "Insurance Platform: Policy expired",
            "PolicyPaymentFailed" => "Insurance Platform: Policy payment failed",
            "PolicyExpiringReminder" => "Insurance Platform: Policy expiry reminder",
            "UserRegistered" => "Welcome to Insurance Platform",
            _ => $"Insurance Platform: {eventType}"
        };

        private static string BuildMessage(string eventType, JsonElement data) => eventType switch
        {
            "TicketUpdated" => $"Ticket status changed: {TryReadString(data, "Status") ?? "Updated"}",
            "TicketAssigned" => "A ticket was assigned.",
            "CommentAdded" => $"A new comment was added to ticket {TryReadString(data, "TicketId") ?? string.Empty}.",
            "ClaimApproved" => "Your claim was approved.",
            "ClaimRejected" => "Your claim was rejected.",
            "PolicyPurchased" => "A new policy was purchased successfully.",
            "PolicyRenewed" => "A policy was renewed successfully.",
            "PolicyExpired" => "Your policy has expired.",
            "PolicyPaymentFailed" => $"Policy payment failed: {TryReadString(data, "Reason") ?? "Payment could not be completed."}",
            "PolicyExpiringReminder" => BuildExpiryReminderMessage(data),
            "UserRegistered" => BuildWelcomeMessage(data),
            _ => $"{eventType}: {data}"
        };

        private static string BuildWelcomeMessage(JsonElement data)
        {
            var name = TryReadString(data, "Name");
            return string.IsNullOrWhiteSpace(name)
                ? "Welcome to Insurance Platform. Your account has been created successfully."
                : $"Welcome to Insurance Platform, {name}. Your account has been created successfully.";
        }

        private static string BuildExpiryReminderMessage(JsonElement data)
        {
            var days = TryReadString(data, "DaysUntilExpiry") ?? "soon";
            var reminderWindow = TryReadString(data, "ReminderWindow") ?? string.Empty;
            return reminderWindow == "FinalWeek"
                ? $"Policy expiry reminder: your policy expires in {days} day(s)."
                : $"Policy expiry reminder: your policy is within one month of expiry and has {days} day(s) left.";
        }
    }
}
