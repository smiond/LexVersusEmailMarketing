namespace LexVersusEmailMarketing.Core.Entities;

public class CampaignRecipient
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;
    public Guid ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
    public RecipientStatus Status { get; set; } = RecipientStatus.Pending;
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
}
