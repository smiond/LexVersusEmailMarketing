namespace LexVersusEmailMarketing.Core.Entities;

public class Campaign
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid EmailTemplateId { get; set; }
    public EmailTemplate EmailTemplate { get; set; } = null!;
    public Guid? EmailListId { get; set; }
    public EmailList? EmailList { get; set; }
    public Guid? SenderIdentityId { get; set; }
    public SenderIdentity? SenderIdentity { get; set; }
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public string? SubjectOverride { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CampaignRecipient> Recipients { get; set; } = [];
}
