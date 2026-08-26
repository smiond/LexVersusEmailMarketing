namespace LexVersusEmailMarketing.Core.Entities;

public class Contact
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsSubscribed { get; set; } = true;
    public string UnsubscribeToken { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime? UnsubscribedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<EmailListContact> EmailListContacts { get; set; } = [];
    public ICollection<CampaignRecipient> CampaignRecipients { get; set; } = [];
}
