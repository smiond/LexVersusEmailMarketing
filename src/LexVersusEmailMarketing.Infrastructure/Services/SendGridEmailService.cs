using LexVersusEmailMarketing.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace LexVersusEmailMarketing.Infrastructure.Services;

public class SendGridEmailService(
    IConfiguration configuration,
    ILogger<SendGridEmailService> logger) : IEmailSenderService
{
    public async Task<(bool Success, string? Error)> SendEmailAsync(
        string toEmail,
        string? toName,
        string subject,
        string htmlBody,
        string? fromEmail = null,
        string? fromName = null,
        string? replyToEmail = null,
        CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["SendGrid:ApiKey"];
        var resolvedFromEmail = fromEmail ?? configuration["SendGrid:FromEmail"];
        var resolvedFromName = fromName
            ?? configuration["SendGrid:FromName"]
            ?? "LexVersus Marketing";

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_SENDGRID_API_KEY")
        {
            logger.LogWarning("SendGrid API key not configured. Email to {Email} not sent.", toEmail);
            return (false, "SendGrid API key is not configured. Paste the full key starting with SG. into SendGrid:ApiKey.");
        }

        if (string.IsNullOrWhiteSpace(resolvedFromEmail))
        {
            return (false, "Sender email is not configured.");
        }

        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(resolvedFromEmail, resolvedFromName);
        var to = new EmailAddress(toEmail, toName);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlBody);

        if (!string.IsNullOrWhiteSpace(replyToEmail))
        {
            msg.ReplyTo = new EmailAddress(replyToEmail);
        }

        var response = await client.SendEmailAsync(msg, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        var body = await response.Body.ReadAsStringAsync(cancellationToken);
        logger.LogError("SendGrid failed for {Email} from {From}: {Status} - {Body}",
            toEmail, resolvedFromEmail, response.StatusCode, body);
        return (false, $"SendGrid error: {response.StatusCode}");
    }
}
