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

                    // Build targeted per-recipient notifications (role-aware, no duplicates)
                    var notifications = BuildNotifications(eventType, data);

                    foreach (var (recipientId, message) in notifications)
                    {
                        await repository.AddAsync(new Notification { UserId = recipientId, Message = message }, stoppingToken);
                    }

                    await repository.SaveChangesAsync(stoppingToken);

                    // Send emails to all named recipients
                    var recipientIds = notifications
                        .Where(n => n.RecipientId.HasValue)
                        .Select(n => n.RecipientId!.Value)
                        .ToHashSet();

                    if (recipientIds.Count > 0)
                    {
                        var subject = BuildSubject(eventType);
                        var contacts = await repository.GetUserContactsAsync(recipientIds, stoppingToken);
                        foreach (var contact in contacts)
                        {
                            // Find the message for this specific recipient
                            var personalMessage = notifications
                                .FirstOrDefault(n => n.RecipientId == contact.UserId).Message
                                ?? notifications.FirstOrDefault(n => n.RecipientId.HasValue).Message
                                ?? string.Empty;
                            await emailSender.SendAsync(contact.Email, subject, personalMessage, stoppingToken);
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

        /// <summary>
        /// Returns a list of (RecipientId, Message) pairs — one per intended recipient.
        /// - RecipientId = specific Guid  → personal notification for that user
        /// - RecipientId = null           → platform-wide alert visible to all admins
        /// No duplicates: each event produces at most one notification per recipient.
        /// </summary>
        private static List<(Guid? RecipientId, string Message)> BuildNotifications(string eventType, JsonElement data)
        {
            var customerId  = TryReadGuid(data, "CustomerId");
            var assignedTo  = TryReadGuid(data, "AssignedTo");
            var changedBy   = TryReadGuid(data, "ChangedBy");
            var commentedBy = TryReadGuid(data, "CommentedBy");
            var userId      = TryReadGuid(data, "UserId");

            return eventType switch
            {
                // ── User events ──────────────────────────────────────────────────────────
                "UserRegistered" => BuildUserRegisteredNotifications(data, userId),

                "UserStatusUpdated" => userId.HasValue
                    ? [(userId, BuildUserStatusMessage(data))]
                    : [],

                // ── Policy catalog events — admin alert only ──────────────────────────────
                "PolicyCreated" => [
                    (null, BuildPolicyCreatedAdminMessage(data))
                ],

                "PolicyUpdated" => [
                    (null, BuildPolicyUpdatedAdminMessage(data))
                ],

                // No notification for PaymentRequested (internal workflow event)
                "PaymentRequested" => [],

                // ── Policy purchase / renewal ────────────────────────────────────────────
                "PolicyPurchased" => BuildPolicyPurchasedNotifications(data, customerId),

                "PolicyRenewed" => customerId.HasValue
                    ? [(customerId, BuildPolicyRenewedMessage(data))]
                    : [],

                "PolicyExpired" => customerId.HasValue
                    ? [(customerId, "Your policy has expired. Please renew it to continue your coverage.")]
                    : [],

                "PolicyPaymentFailed" => customerId.HasValue
                    ? [(customerId, $"Policy payment failed. Reason: {TryReadString(data, "Reason") ?? "Payment declined"}. Please retry or contact support.")]
                    : [],

                "PolicyExpiringReminder" => customerId.HasValue
                    ? [(customerId, BuildExpiryReminderMessage(data))]
                    : [],

                // ── Payment events ───────────────────────────────────────────────────────
                "PaymentCompleted" => BuildPaymentCompletedNotifications(data, customerId),

                "PaymentFailed" => customerId.HasValue
                    ? [(customerId, BuildPaymentFailedMessage(data))]
                    : [],

                // ── Ticket events ────────────────────────────────────────────────────────
                "TicketCreated" => BuildTicketCreatedNotifications(data, customerId),

                "TicketAssigned" => BuildTicketAssignedNotifications(data, customerId, assignedTo),

                "TicketUpdated" => BuildTicketUpdatedNotifications(data, customerId, assignedTo, changedBy),

                "CommentAdded" => BuildCommentAddedNotifications(data, customerId, assignedTo, commentedBy),

                "ClaimApproved" => BuildClaimDecisionNotifications(data, customerId, approved: true),

                "ClaimRejected" => BuildClaimDecisionNotifications(data, customerId, approved: false),

                // ── OTP events ───────────────────────────────────────────────────────────
                "OtpForgotPassword" => BuildOtpNotifications(data, userId, isForgotPassword: true),

                "OtpEmailVerification" => BuildOtpNotifications(data, userId, isForgotPassword: false),

                // Unknown events — silently discard
                _ => []
            };
        }

        // ── Per-event notification builders ─────────────────────────────────────────────

        private static List<(Guid?, string)> BuildUserRegisteredNotifications(JsonElement data, Guid? userId)
        {
            var result = new List<(Guid?, string)>();
            var name = TryReadString(data, "Name");
            var role = TryReadString(data, "Role");

            // Welcome message to the new user
            if (userId.HasValue)
                result.Add((userId, BuildWelcomeMessage(data)));

            // Admin alert: new user joined
            var roleLabel = role switch { "ClaimsSpecialist" => "Claims Specialist", "SupportSpecialist" => "Support Specialist", _ => role ?? "User" };
            result.Add((null, $"New {roleLabel} registered: {name ?? "Unknown"}. Review in Users & Roles."));

            return result;
        }

        private static List<(Guid?, string)> BuildPolicyPurchasedNotifications(JsonElement data, Guid? customerId)
        {
            var result = new List<(Guid?, string)>();
            var vehicleNumber = TryReadString(data, "VehicleNumber");

            // Customer: their policy is active
            if (customerId.HasValue)
                result.Add((customerId, string.IsNullOrWhiteSpace(vehicleNumber)
                    ? "Your policy has been purchased successfully. Your coverage is now active."
                    : $"Your policy for vehicle {vehicleNumber} has been purchased successfully. Your coverage is now active."));

            // Admin: platform activity
            result.Add((null, string.IsNullOrWhiteSpace(vehicleNumber)
                ? "A new policy has been purchased by a customer."
                : $"New policy purchased for vehicle {vehicleNumber}."));

            return result;
        }

        private static List<(Guid?, string)> BuildPaymentCompletedNotifications(JsonElement data, Guid? customerId)
        {
            var result = new List<(Guid?, string)>();
            var amount = TryReadString(data, "Amount");
            var reference = TryReadString(data, "PaymentReference");
            var refPart = string.IsNullOrWhiteSpace(reference) ? string.Empty : $" (Ref: {reference})";

            // Customer: payment confirmed
            if (customerId.HasValue)
                result.Add((customerId, $"Payment of ₹{amount ?? "—"} received successfully{refPart}. Thank you!"));

            // Admin: revenue alert
            result.Add((null, $"Payment of ₹{amount ?? "—"} completed{refPart}."));

            return result;
        }

        private static List<(Guid?, string)> BuildTicketCreatedNotifications(JsonElement data, Guid? customerId)
        {
            var result = new List<(Guid?, string)>();
            var ticketType = TryReadString(data, "Type");
            var isClaim = ticketType == "Claim";

            // Customer: submission confirmed
            if (customerId.HasValue)
                result.Add((customerId, isClaim
                    ? "Your claim has been submitted successfully. A Claims Specialist will review it shortly."
                    : "Your support ticket has been submitted successfully. Our team will review it shortly."));

            // Admin: new ticket needs assignment
            result.Add((null, isClaim
                ? "A new claim ticket has been submitted and needs to be assigned to a Claims Specialist."
                : "A new support ticket has been submitted and needs to be assigned to a Support Specialist."));

            return result;
        }

        private static List<(Guid?, string)> BuildTicketAssignedNotifications(
            JsonElement data, Guid? customerId, Guid? assignedTo)
        {
            var result = new List<(Guid?, string)>();
            var ticketType = TryReadString(data, "Type");
            var isClaim = ticketType == "Claim";

            // Customer: their ticket was assigned
            if (customerId.HasValue)
                result.Add((customerId, isClaim
                    ? "Your claim ticket has been assigned to a Claims Specialist who will review it shortly."
                    : "Your support ticket has been assigned to a Support Specialist who will assist you shortly."));

            // Assigned specialist: new ticket in their queue
            if (assignedTo.HasValue && assignedTo != customerId)
                result.Add((assignedTo, isClaim
                    ? "A new claim ticket has been assigned to you. Please review it in your dashboard."
                    : "A new support ticket has been assigned to you. Please review it in your dashboard."));

            return result;
        }

        private static List<(Guid?, string)> BuildTicketUpdatedNotifications(
            JsonElement data, Guid? customerId, Guid? assignedTo, Guid? changedBy)
        {
            var result = new List<(Guid?, string)>();
            var status = TryReadString(data, "Status");

            var customerMsg = status switch
            {
                "Resolved" => "Great news! Your ticket has been resolved. Log in to view the resolution.",
                "Closed"   => "Your ticket has been closed. If you need further assistance, please raise a new ticket.",
                "Assigned" => "Your ticket has been assigned to a specialist who will review it shortly.",
                "InReview" => "Your ticket is currently under review by our team.",
                "Rejected" => "Your ticket has been reviewed and closed. Please contact support if you need further help.",
                _          => $"Your ticket status has been updated to: {status ?? "Updated"}."
            };

            if (customerId.HasValue)
                result.Add((customerId, customerMsg));

            // Notify assigned specialist only if they didn't make the change
            if (assignedTo.HasValue && assignedTo != customerId && assignedTo != changedBy)
                result.Add((assignedTo, $"A ticket assigned to you has been updated. New status: {status ?? "Updated"}."));

            return result;
        }

        private static List<(Guid?, string)> BuildCommentAddedNotifications(
            JsonElement data, Guid? customerId, Guid? assignedTo, Guid? commentedBy)
        {
            var result = new List<(Guid?, string)>();

            if (customerId.HasValue && customerId != commentedBy)
                result.Add((customerId, "A specialist has added a comment to your ticket. Log in to view and respond."));

            if (assignedTo.HasValue && assignedTo != commentedBy && assignedTo != customerId)
                result.Add((assignedTo, "A new comment has been added to a ticket assigned to you. Log in to view it."));

            return result;
        }

        private static List<(Guid?, string)> BuildClaimDecisionNotifications(
            JsonElement data, Guid? customerId, bool approved)
        {
            var result = new List<(Guid?, string)>();

            if (approved)
            {
                if (customerId.HasValue)
                    result.Add((customerId, BuildClaimApprovedMessage(data)));
                // Admin: claim approved
                result.Add((null, "A claim has been approved by a Claims Specialist."));
            }
            else
            {
                if (customerId.HasValue)
                    result.Add((customerId, "Your claim has been reviewed and unfortunately could not be approved. Please contact support for more details."));
                // Admin: claim rejected
                result.Add((null, "A claim has been rejected by a Claims Specialist."));
            }

            return result;
        }

        // ── Message helpers ──────────────────────────────────────────────────────────────

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
            "UserRegistered"          => "Welcome to Trust Guard",
            "UserStatusUpdated"       => "Trust Guard: Account status updated",
            "PolicyCreated"           => "Trust Guard: New policy added",
            "PolicyUpdated"           => "Trust Guard: Policy updated",
            "PolicyPurchased"         => "Trust Guard: Policy purchased",
            "PolicyRenewed"           => "Trust Guard: Policy renewed",
            "PolicyExpired"           => "Trust Guard: Policy expired",
            "PolicyPaymentFailed"     => "Trust Guard: Payment failed",
            "PolicyExpiringReminder"  => "Trust Guard: Policy expiring soon",
            "PaymentCompleted"        => "Trust Guard: Payment received",
            "PaymentFailed"           => "Trust Guard: Payment failed",
            "TicketCreated"           => "Trust Guard: Ticket submitted",
            "TicketUpdated"           => "Trust Guard: Ticket status updated",
            "TicketAssigned"          => "Trust Guard: Ticket assigned",
            "CommentAdded"            => "Trust Guard: New comment on ticket",
            "ClaimApproved"           => "Trust Guard: Claim approved ✓",
            "ClaimRejected"           => "Trust Guard: Claim rejected",
            "OtpForgotPassword"       => "Trust Guard: Password Reset OTP",
            "OtpEmailVerification"    => "Trust Guard: Email Verification OTP",
            _                         => $"Trust Guard: {eventType}"
        };

        private static string BuildWelcomeMessage(JsonElement data)
        {
            var name = TryReadString(data, "Name");
            return string.IsNullOrWhiteSpace(name)
                ? "Welcome to Trust Guard! Your account has been created successfully."
                : $"Welcome to Trust Guard, {name}! Your account has been created successfully. You can now browse and purchase policies.";
        }

        private static string BuildUserStatusMessage(JsonElement data)
        {
            var isActive = TryReadString(data, "IsActive");
            return isActive == "True" || isActive == "true"
                ? "Your account has been activated. You can now log in and access all services."
                : "Your account has been deactivated. Please contact support if you believe this is an error.";
        }

        private static string BuildPolicyCreatedAdminMessage(JsonElement data)
        {
            var name = TryReadString(data, "Name");
            var vehicleType = TryReadString(data, "VehicleType");
            var premium = TryReadString(data, "Premium");
            return $"New {vehicleType} policy \"{name}\" added to the catalog at ₹{premium}/year.";
        }

        private static string BuildPolicyUpdatedAdminMessage(JsonElement data)
        {
            var name = TryReadString(data, "Name");
            return $"Policy \"{name ?? "Unknown"}\" has been updated in the catalog.";
        }

        private static string BuildPolicyPurchasedMessage(JsonElement data)
        {
            var vehicleNumber = TryReadString(data, "VehicleNumber");
            return string.IsNullOrWhiteSpace(vehicleNumber)
                ? "Your policy has been purchased successfully. Your coverage is now active."
                : $"Your policy for vehicle {vehicleNumber} has been purchased successfully. Your coverage is now active.";
        }

        private static string BuildPolicyRenewedMessage(JsonElement data)
        {
            var vehicleNumber = TryReadString(data, "VehicleNumber");
            return string.IsNullOrWhiteSpace(vehicleNumber)
                ? "Your policy has been renewed successfully. Your coverage has been extended for another year."
                : $"Your policy for vehicle {vehicleNumber} has been renewed successfully. Your coverage has been extended for another year.";
        }

        private static string BuildPaymentCompletedMessage(JsonElement data)
        {
            var amount = TryReadString(data, "Amount");
            var reference = TryReadString(data, "PaymentReference");
            var refPart = string.IsNullOrWhiteSpace(reference) ? string.Empty : $" (Ref: {reference})";
            return $"Payment of ₹{amount ?? "—"} received successfully{refPart}. Thank you!";
        }

        private static string BuildPaymentFailedMessage(JsonElement data)
        {
            var amount = TryReadString(data, "Amount");
            var reason = TryReadString(data, "Reason");
            var reasonPart = string.IsNullOrWhiteSpace(reason) ? string.Empty : $" Reason: {reason}.";
            return $"Your payment of ₹{amount ?? "—"} could not be processed.{reasonPart} Please retry or contact support.";
        }

        private static string BuildClaimApprovedMessage(JsonElement data)
        {
            var amount = TryReadString(data, "Amount");
            return string.IsNullOrWhiteSpace(amount)
                ? "Your claim has been approved! The settlement will be processed shortly."
                : $"Your claim has been approved! A settlement of ₹{amount} will be processed shortly.";
        }

        private static string BuildExpiryReminderMessage(JsonElement data)
        {
            var days = TryReadString(data, "DaysUntilExpiry") ?? "soon";
            var reminderWindow = TryReadString(data, "ReminderWindow") ?? string.Empty;
            return reminderWindow == "FinalWeek"
                ? $"Urgent: Your policy expires in {days} day(s). Please renew now to avoid a lapse in coverage."
                : $"Reminder: Your policy expires in {days} day(s). Log in to renew and keep your coverage active.";
        }

        private static List<(Guid?, string)> BuildOtpNotifications(JsonElement data, Guid? userId, bool isForgotPassword)
        {
            if (!userId.HasValue) return [];
            var code = TryReadString(data, "Code") ?? "------";
            var expiry = TryReadString(data, "ExpiryMinutes") ?? "10";
            var name = TryReadString(data, "Name");
            var greeting = string.IsNullOrWhiteSpace(name) ? "Hello" : $"Hello {name}";

            var message = isForgotPassword
                ? $"{greeting}, your password reset OTP is: {code}. It expires in {expiry} minutes. Do not share this code with anyone."
                : $"{greeting}, your email verification OTP is: {code}. It expires in {expiry} minutes. Enter this code to verify your account.";

            return [(userId, message)];
        }
    }
}
