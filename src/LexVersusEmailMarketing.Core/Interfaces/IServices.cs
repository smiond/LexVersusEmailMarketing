using LexVersusEmailMarketing.Core.DTOs;

namespace LexVersusEmailMarketing.Core.Interfaces;

public interface IContactImportService
{
    Task<ImportResultDto> ImportFromFileAsync(Stream fileStream, string fileName, Guid? emailListId, CancellationToken cancellationToken = default);
}

public interface IEmailSenderService
{
    Task<(bool Success, string? Error)> SendEmailAsync(
        string toEmail,
        string? toName,
        string subject,
        string htmlBody,
        string? fromEmail = null,
        string? fromName = null,
        string? replyToEmail = null,
        CancellationToken cancellationToken = default);
}

public interface ICampaignService
{
    Task<CampaignSendResultDto> SendCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default);
}
