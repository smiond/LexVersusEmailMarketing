namespace LexVersusEmailMarketing.Core.Entities;

public enum CampaignStatus
{
    Draft,
    Scheduled,
    Sending,
    Sent,
    Failed,
    Cancelled
}

public enum RecipientStatus
{
    Pending,
    Sent,
    Failed,
    Skipped
}
