using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using NotificationService.Config;

namespace NotificationService.Services;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken);
}

public sealed class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;
    private readonly ILogger<SmtpEmailSender> _logger = logger;

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            _logger.LogWarning("Email delivery is enabled but SMTP settings are incomplete.");
            return;
        }

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        message.To.Add(toEmail);
        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message);
    }
}
