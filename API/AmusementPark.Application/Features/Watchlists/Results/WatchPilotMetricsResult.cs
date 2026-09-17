using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Results;

public sealed record WatchPilotMetricsResult(
    DateTime GeneratedAtUtc,
    DateTime FromUtc,
    DateTime ToUtc,
    long ActiveSubscriptions,
    IReadOnlyDictionary<string, long> ActiveSubscriptionsByEventType,
    long EventsVerified,
    long EventsPublished,
    long EventsCorrected,
    long EventsRetracted,
    long NotificationsDelivered,
    long DuplicateNotifications,
    long NotificationCenterOpens,
    long SourceOpens,
    long MisleadingAlertReports,
    long SubscriptionsRemoved,
    long DigestsGenerated,
    long EmailPending,
    long EmailSucceeded,
    long EmailFailed,
    long EmailCancelled,
    long? BounceCount,
    long? ComplaintCount,
    long PendingOutboxEntries,
    IReadOnlyDictionary<string, long> QueueCountsByStatus,
    WatchPilotHealth Health,
    IReadOnlyCollection<WatchPilotDailyMetrics> Daily);
