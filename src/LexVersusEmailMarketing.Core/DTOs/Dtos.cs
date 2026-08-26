namespace LexVersusEmailMarketing.Core.DTOs;

public record ContactDto(Guid Id, string Email, string? FirstName, string? LastName, bool IsSubscribed, DateTime CreatedAt);

public record CreateContactDto(string Email, string? FirstName, string? LastName);

public record EmailListDto(Guid Id, string Name, string? Description, int ContactCount, DateTime CreatedAt);

public record CreateEmailListDto(string Name, string? Description);

public record ImportResultDto(int Imported, int Skipped, int Duplicates, List<string> Errors);

public record EmailTemplateDto(Guid Id, string Name, string Subject, string HtmlBody, DateTime CreatedAt, DateTime UpdatedAt);

public record CreateEmailTemplateDto(string Name, string Subject, string HtmlBody);

public record UpdateEmailTemplateDto(string Name, string Subject, string HtmlBody);

public record SenderIdentityDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? ReplyToEmail,
    bool IsDefault,
    bool IsActive,
    DateTime CreatedAt);

public record CreateSenderIdentityDto(string Email, string DisplayName, string? ReplyToEmail, bool IsDefault);

public record UpdateSenderIdentityDto(string Email, string DisplayName, string? ReplyToEmail, bool IsDefault, bool IsActive);

public record CampaignDto(
    Guid Id,
    string Name,
    Guid EmailTemplateId,
    string EmailTemplateName,
    Guid? EmailListId,
    string? EmailListName,
    Guid? SenderIdentityId,
    string? SenderEmail,
    string? SenderDisplayName,
    string Status,
    string? SubjectOverride,
    DateTime? ScheduledAt,
    DateTime? SentAt,
    DateTime CreatedAt,
    int TotalRecipients,
    int SentCount,
    int FailedCount);

public record CreateCampaignDto(
    string Name,
    Guid EmailTemplateId,
    Guid? EmailListId,
    Guid? SenderIdentityId,
    string? SubjectOverride,
    DateTime? ScheduledAt);

public record CampaignSendResultDto(Guid CampaignId, string Status, int Sent, int Failed, int Total);

public record DashboardStatsDto(
    int TotalContacts,
    int TotalTemplates,
    int TotalCampaigns,
    int ActiveCampaigns,
    int EmailsSentToday);
