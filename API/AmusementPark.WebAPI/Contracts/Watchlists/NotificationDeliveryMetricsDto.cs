namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed record NotificationDeliveryMetricsDto(
    DateTime FromUtc,
    DateTime ToUtc,
    long PendingCount,
    long FailedCount,
    long SucceededCount,
    long CancelledCount,
    DateTime? LatestFailureAtUtc);
