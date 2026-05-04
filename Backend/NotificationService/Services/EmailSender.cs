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

        var htmlBody = BuildHtmlEmail(subject, body);

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        // Plain-text fallback
        var plainView = AlternateView.CreateAlternateViewFromString(body, null, "text/plain");
        var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
        message.AlternateViews.Add(plainView);
        message.AlternateViews.Add(htmlView);

        message.To.Add(toEmail);
        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message);
    }

    private static string BuildHtmlEmail(string subject, string body)
    {
        // Escape any HTML special characters in the body text
        var safeBody = System.Net.WebUtility.HtmlEncode(body)
            .Replace("&#xA;", "<br>")
            .Replace("\n", "<br>");

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <title>{System.Net.WebUtility.HtmlEncode(subject)}</title>
            </head>
            <body style="margin:0;padding:0;background-color:#F5F7FA;font-family:'Segoe UI',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#F5F7FA;padding:32px 0;">
                <tr>
                  <td align="center">
                    <table width="600" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 12px rgba(0,0,0,0.08);">

                      <!-- Header -->
                      <tr>
                        <td style="background:#002045;padding:24px 32px;">
                          <table width="100%" cellpadding="0" cellspacing="0">
                            <tr>
                              <td>
                                <table cellpadding="0" cellspacing="0">
                                  <tr>
                                    <td style="vertical-align:middle;padding-right:10px;">
                                      <!-- Shield SVG — matches the BrandLogo component exactly -->
                                      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="36" height="36" style="display:block;">
                                        <path d="M12 2 L3 6.5 V12 C3 16.9 7 21.5 12 23 C17 21.5 21 16.9 21 12 V6.5 Z" fill="#ffffff"/>
                                        <path d="M9 12.5 L11 14.5 L15 10.5" stroke="#002045" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" fill="none"/>
                                      </svg>
                                    </td>
                                    <td style="vertical-align:middle;">
                                      <span style="color:#ffffff;font-size:22px;font-weight:700;letter-spacing:-0.01em;font-family:'Segoe UI',Arial,sans-serif;">
                                        Trust Guard
                                      </span>
                                    </td>
                                  </tr>
                                </table>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <!-- Body -->
                      <tr>
                        <td style="padding:32px;">
                          <h2 style="margin:0 0 16px 0;color:#002045;font-size:20px;font-weight:600;">
                            {System.Net.WebUtility.HtmlEncode(subject).Replace("Trust Guard: ", "").Replace("Trust Guard — ", "")}
                          </h2>
                          <p style="margin:0 0 24px 0;color:#374151;font-size:15px;line-height:1.7;">
                            {safeBody}
                          </p>
                          <table cellpadding="0" cellspacing="0">
                            <tr>
                              <td style="background:#002045;border-radius:8px;padding:12px 28px;">
                                <a href="http://localhost:5173" style="color:#ffffff;text-decoration:none;font-size:14px;font-weight:600;">
                                  Log in to Trust Guard →
                                </a>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <!-- Divider -->
                      <tr>
                        <td style="padding:0 32px;">
                          <hr style="border:none;border-top:1px solid #E5E7EB;margin:0;">
                        </td>
                      </tr>

                      <!-- Footer -->
                      <tr>
                        <td style="padding:20px 32px;background:#F9FAFB;">
                          <p style="margin:0;color:#9CA3AF;font-size:12px;line-height:1.6;">
                            This is an automated notification from Trust Guard.<br>
                            If you did not expect this email, please ignore it or contact support.
                          </p>
                        </td>
                      </tr>

                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }
}
