namespace LexVersusEmailMarketing.Core.Entities;

public class EmailListContact
{
    public Guid EmailListId { get; set; }
    public EmailList EmailList { get; set; } = null!;

    public Guid ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
}
