namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed record LiveAlertDashboardDto(
    IReadOnlyCollection<LiveAlertSubscriptionDto> Subscriptions,
    IReadOnlyCollection<LiveAlertNotificationDto> Notifications,
    int UnreadCount,
    int RetentionDays);
