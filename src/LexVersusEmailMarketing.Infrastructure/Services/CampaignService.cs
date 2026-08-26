using LexVersusEmailMarketing.Core.DTOs;
using LexVersusEmailMarketing.Core.Entities;
using LexVersusEmailMarketing.Core.Interfaces;
using LexVersusEmailMarketing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LexVersusEmailMarketing.Infrastructure.Services;

public class CampaignService(AppDbContext db, IEmailSenderService emailSender, IConfiguration configuration) : ICampaignService
{
    private readonly string _publicUrl = configuration["App:PublicUrl"] ?? "http://localhost:5173";

    public async Task<CampaignSendResultDto> SendCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await db.Campaigns
            .Include(c => c.EmailTemplate)
            .Include(c => c.SenderIdentity)
            .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken)
            ?? throw new InvalidOperationException("Campaign not found.");

        if (campaign.Status is CampaignStatus.Sending or CampaignStatus.Sent)
        {
            throw new InvalidOperationException($"Campaign is already {campaign.Status}.");
        }

        var existingRecipientCount = await db.CampaignRecipients
            .CountAsync(r => r.CampaignId == campaignId, cancellationToken);

        if (existingRecipientCount == 0)
        {
            await PrepareRecipientsAsync(campaignId, campaign.EmailListId, cancellationToken);
        }

        var (fromEmail, fromName, replyTo) = await ResolveSenderAsync(campaign, cancellationToken);

        campaign.Status = CampaignStatus.Sending;
        await db.SaveChangesAsync(cancellationToken);

        var recipients = await db.CampaignRecipients
            .Include(r => r.Contact)
            .Where(r => r.CampaignId == campaignId && r.Status == RecipientStatus.Pending)
            .ToListAsync(cancellationToken);

        if (recipients.Count == 0)
        {
            campaign.Status = CampaignStatus.Failed;
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("No subscribed contacts found to send this campaign.");
        }

        var subject = campaign.SubjectOverride ?? campaign.EmailTemplate.Subject;
        var sent = 0;
        var failed = 0;

        foreach (var recipient in recipients)
        {
            if (!recipient.Contact.IsSubscribed)
            {
                recipient.Status = RecipientStatus.Skipped;
                await db.SaveChangesAsync(cancellationToken);
                continue;
            }

            var htmlBody = BuildHtmlBody(campaign.EmailTemplate.HtmlBody, recipient.Contact);
            var subjectLine = Personalize(subject, recipient.Contact);

            var (success, error) = await emailSender.SendEmailAsync(
                recipient.Contact.Email,
                GetDisplayName(recipient.Contact),
                subjectLine,
                htmlBody,
                fromEmail,
                fromName,
                replyTo,
                cancellationToken);

            if (success)
            {
                recipient.Status = RecipientStatus.Sent;
                recipient.SentAt = DateTime.UtcNow;
                sent++;
            }
            else
            {
                recipient.Status = RecipientStatus.Failed;
                recipient.ErrorMessage = error;
                failed++;
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        var total = await db.CampaignRecipients.CountAsync(r => r.CampaignId == campaignId, cancellationToken);
        campaign.Status = failed > 0 && sent == 0 ? CampaignStatus.Failed : CampaignStatus.Sent;
        campaign.SentAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new CampaignSendResultDto(campaign.Id, campaign.Status.ToString(), sent, failed, total);
    }

    private async Task<(string FromEmail, string FromName, string? ReplyTo)> ResolveSenderAsync(Campaign campaign, CancellationToken cancellationToken)
    {
        if (campaign.SenderIdentity is { IsActive: true })
        {
            return (campaign.SenderIdentity.Email, campaign.SenderIdentity.DisplayName, campaign.SenderIdentity.ReplyToEmail);
        }

        if (campaign.SenderIdentityId.HasValue)
        {
            throw new InvalidOperationException("Selected sender identity is inactive or missing.");
        }

        var defaultSender = await db.SenderIdentities
            .AsNoTracking()
            .Where(s => s.IsActive && s.IsDefault)
            .OrderBy(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultSender is not null)
        {
            return (defaultSender.Email, defaultSender.DisplayName, defaultSender.ReplyToEmail);
        }

        var anySender = await db.SenderIdentities
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (anySender is not null)
        {
            return (anySender.Email, anySender.DisplayName, anySender.ReplyToEmail);
        }

        var configEmail = configuration["SendGrid:FromEmail"];
        var configName = configuration["SendGrid:FromName"] ?? "LexVersus Marketing";
        if (string.IsNullOrWhiteSpace(configEmail) || configEmail == "marketing@yourdomain.com")
        {
            throw new InvalidOperationException(
                "No sender identity configured. Add a verified SendGrid sender under Senders, or set SendGrid:FromEmail.");
        }

        return (configEmail, configName, null);
    }

    private async Task PrepareRecipientsAsync(Guid campaignId, Guid? emailListId, CancellationToken cancellationToken)
    {
        var contactQuery = db.Contacts.AsNoTracking().Where(c => c.IsSubscribed);

        if (emailListId.HasValue)
        {
            contactQuery = contactQuery.Where(c =>
                c.EmailListContacts.Any(elc => elc.EmailListId == emailListId));
        }

        var contactIds = await contactQuery.Select(c => c.Id).ToListAsync(cancellationToken);
        if (contactIds.Count == 0)
        {
            throw new InvalidOperationException("No subscribed contacts found to send this campaign.");
        }

        var recipients = contactIds.Select(contactId => new CampaignRecipient
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            ContactId = contactId,
            Status = RecipientStatus.Pending
        }).ToList();

        db.CampaignRecipients.AddRange(recipients);
        await db.SaveChangesAsync(cancellationToken);
    }

    private string BuildHtmlBody(string template, Contact contact)
    {
        var unsubscribeUrl = $"{_publicUrl}/unsubscribe/{contact.UnsubscribeToken}";
        var personalized = Personalize(template, contact)
            .Replace("{{unsubscribeUrl}}", unsubscribeUrl, StringComparison.OrdinalIgnoreCase);

        if (!personalized.Contains("unsubscribe", StringComparison.OrdinalIgnoreCase))
        {
            personalized += $"""
                <hr style="margin-top:32px;border:none;border-top:1px solid #ddd;" />
                <p style="font-size:12px;color:#888;text-align:center;">
                  You received this email because you are subscribed to our mailing list.
                  <a href="{unsubscribeUrl}" style="color:#888;">Unsubscribe</a>
                </p>
                """;
        }

        return personalized;
    }

    private static string Personalize(string template, Contact contact)
    {
        return template
            .Replace("{{email}}", contact.Email, StringComparison.OrdinalIgnoreCase)
            .Replace("{{firstName}}", contact.FirstName ?? "", StringComparison.OrdinalIgnoreCase)
            .Replace("{{lastName}}", contact.LastName ?? "", StringComparison.OrdinalIgnoreCase)
            .Replace("{{fullName}}", GetDisplayName(contact), StringComparison.OrdinalIgnoreCase);
    }

    private static string GetDisplayName(Contact contact)
    {
        var parts = new[] { contact.FirstName, contact.LastName }.Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(" ", parts);
    }
}
