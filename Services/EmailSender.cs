using System.Net;
using System.Net.Mail;

namespace SmartELibrary.Services;

public interface IEmailSender
{
    bool IsConfigured { get; }
    Task SendAsync(string recipientEmail, string subject, string htmlBody);
}

public class SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public bool IsConfigured
        => !string.IsNullOrWhiteSpace(configuration["Email:SmtpHost"])
           && !string.IsNullOrWhiteSpace(configuration["Email:FromAddress"]);

    public async Task SendAsync(string recipientEmail, string subject, string htmlBody)
    {
        if (!IsConfigured)
        {
            logger.LogWarning("SMTP is not configured. Skipping email send to {Recipient}.", recipientEmail);
            return;
        }

        var host = configuration["Email:SmtpHost"] ?? string.Empty;
        var port = int.TryParse(configuration["Email:SmtpPort"], out var parsedPort) ? parsedPort : 587;
        var username = configuration["Email:Username"] ?? string.Empty;
        var password = configuration["Email:Password"] ?? string.Empty;
        var enableSsl = !string.Equals(configuration["Email:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase);
        var fromAddress = configuration["Email:FromAddress"] ?? string.Empty;
        var fromName = configuration["Email:FromName"] ?? "EduVault";

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(recipientEmail);

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = string.IsNullOrWhiteSpace(username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(username, password)
        };

        await client.SendMailAsync(message);
    }
}