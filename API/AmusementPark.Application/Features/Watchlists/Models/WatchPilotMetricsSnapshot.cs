namespace AmusementPark.Application.Features.Watchlists.Models;

public sealed record WatchPilotMetricsSnapshot(
    long ActiveSubscriptions,
    IReadOnlyDictionary<string, long> ActiveSubscriptionsByEventType,
    long EventsVerified,
    long EventsPublished,
    long EventsCorrected,
    long EventsRetracted,
    long NotificationsDelivered,
    long DuplicateNotifications,
    decimal AverageDeliveryLatencySeconds,
    long DigestsGenerated,
    long EmailPending,
    long EmailSucceeded,
    long EmailFailed,
    long EmailCancelled,
    bool ProviderFeedbackAvailable,
    long? BounceCount,
    long? ComplaintCount,
    long PendingOutboxEntries,
    IReadOnlyDictionary<string, long> QueueCountsByStatus,
    IReadOnlyCollection<WatchPilotDailyMetrics> Daily);
