namespace LexVersusEmailMarketing.Core.Entities;

public class SenderIdentity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ReplyToEmail { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Campaign> Campaigns { get; set; } = [];
}
