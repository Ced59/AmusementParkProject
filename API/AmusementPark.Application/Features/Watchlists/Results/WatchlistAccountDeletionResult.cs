namespace AmusementPark.Application.Features.Watchlists.Results;

public sealed record WatchlistAccountDeletionResult(
    long CollectionEntryCount,
    long SubscriptionCount,
    long NotificationCount,
    long DigestCount,
    long EmailPreferenceCount,
    long DeliveryAttemptCount,
    long BackgroundJobCount)
{
    public long PurgedDocumentCount =>
        this.CollectionEntryCount
        + this.SubscriptionCount
        + this.NotificationCount
        + this.DigestCount
        + this.EmailPreferenceCount
        + this.DeliveryAttemptCount
        + this.BackgroundJobCount;
}
