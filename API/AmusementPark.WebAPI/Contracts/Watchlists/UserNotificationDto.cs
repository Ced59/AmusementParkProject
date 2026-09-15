namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed class UserNotificationDto
{
    public string NotificationId { get; init; } = string.Empty;

    public string EventType { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public UserNotificationTargetDto Target { get; init; } = new UserNotificationTargetDto();

    public UserNotificationFactValueDto? PreviousValue { get; init; }

    public UserNotificationFactValueDto? NewValue { get; init; }

    public UserNotificationSourceDto Source { get; init; } = new UserNotificationSourceDto();

    public DateTime OccurredAtUtc { get; init; }

    public DateTime DeliveredAtUtc { get; init; }

    public DateTime? ReadAtUtc { get; init; }

    public DateTime ExpiresAtUtc { get; init; }

    public long Version { get; init; }

    public bool CanManageSubscription { get; init; }

    public long? SubscriptionVersion { get; init; }
}
