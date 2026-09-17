namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed class WatchPilotMetricsDto
{
    public DateTime GeneratedAtUtc { get; init; }

    public DateTime FromUtc { get; init; }

    public DateTime ToUtc { get; init; }

    public long ActiveSubscriptions { get; init; }

    public IReadOnlyDictionary<string, long> ActiveSubscriptionsByEventType { get; init; } =
        new Dictionary<string, long>(StringComparer.Ordinal);

    public long EventsVerified { get; init; }

    public long EventsPublished { get; init; }

    public long EventsCorrected { get; init; }

    public long EventsRetracted { get; init; }

    public long NotificationsDelivered { get; init; }

    public long DuplicateNotifications { get; init; }

    public long NotificationCenterOpens { get; init; }

    public long SourceOpens { get; init; }

    public long MisleadingAlertReports { get; init; }

    public long SubscriptionsRemoved { get; init; }

    public long DigestsGenerated { get; init; }

    public long EmailPending { get; init; }

    public long EmailSucceeded { get; init; }

    public long EmailFailed { get; init; }

    public long EmailCancelled { get; init; }

    public long? BounceCount { get; init; }

    public long? ComplaintCount { get; init; }

    public long PendingOutboxEntries { get; init; }

    public IReadOnlyDictionary<string, long> QueueCountsByStatus { get; init; } =
        new Dictionary<string, long>(StringComparer.Ordinal);

    public WatchPilotHealthDto Health { get; init; } = new WatchPilotHealthDto();

    public IReadOnlyCollection<WatchPilotDailyMetricsDto> Daily { get; init; } =
        Array.Empty<WatchPilotDailyMetricsDto>();
}
