namespace AmusementPark.Application.Features.Watchlists.Results;

public sealed record NotificationDeliveryMetricsResult(
    DateTime FromUtc,
    DateTime ToUtc,
    long PendingCount,
    long FailedCount,
    long SucceededCount,
    long CancelledCount,
    DateTime? LatestFailureAtUtc);
