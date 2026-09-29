namespace AmusementPark.Application.Features.Watchlists.Results;

public sealed record LiveAlertDashboardResult(
    IReadOnlyCollection<LiveAlertSubscriptionResult> Subscriptions,
    IReadOnlyCollection<LiveAlertNotificationResult> Notifications,
    int UnreadCount,
    int RetentionDays);
