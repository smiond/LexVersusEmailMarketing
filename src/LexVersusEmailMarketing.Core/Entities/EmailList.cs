namespace LexVersusEmailMarketing.Core.Entities;

public class EmailList
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<EmailListContact> EmailListContacts { get; set; } = [];
}
