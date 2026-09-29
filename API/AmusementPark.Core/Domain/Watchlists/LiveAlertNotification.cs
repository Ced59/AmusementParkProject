using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Core.Domain.Watchlists;

public sealed class LiveAlertNotification
{
    public const int RetentionDays = 30;

    private LiveAlertNotification(
        LiveAlertNotificationId id,
        string userId,
        LiveAlertSubscriptionId subscriptionId,
        string targetId,
        string parkId,
        LiveAlertType type,
        int? thresholdMinutes,
        LiveOperationalStatus? previousStatus,
        LiveOperationalStatus currentStatus,
        int? previousWaitMinutes,
        int? currentWaitMinutes,
        LiveDataSourceId sourceId,
        DateTime observedAtUtc,
        DateTime deliveredAtUtc,
        long ageSeconds,
        UserNotificationStatus status,
        DateTime? readAtUtc,
        DateTime? dismissedAtUtc,
        DateTime expiresAtUtc,
        long version)
    {
        _ = id.Value;
        _ = subscriptionId.Value;
        this.UserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        this.TargetId = IdentifierRules.NormalizeRequired(targetId, nameof(targetId));
        this.ParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        if (!Enum.IsDefined(type) || !Enum.IsDefined(currentStatus) || !Enum.IsDefined(status)
            || ageSeconds < 0 || version < 1)
        {
            throw new ArgumentException("The live alert notification state is invalid.");
        }

        EnsureUtc(observedAtUtc, nameof(observedAtUtc));
        EnsureUtc(deliveredAtUtc, nameof(deliveredAtUtc));
        EnsureUtc(expiresAtUtc, nameof(expiresAtUtc));
        if (readAtUtc.HasValue)
        {
            EnsureUtc(readAtUtc.Value, nameof(readAtUtc));
        }

        if (dismissedAtUtc.HasValue)
        {
            EnsureUtc(dismissedAtUtc.Value, nameof(dismissedAtUtc));
        }

        this.Id = id;
        this.SubscriptionId = subscriptionId;
        this.Type = type;
        this.ThresholdMinutes = thresholdMinutes;
        this.PreviousStatus = previousStatus;
        this.CurrentStatus = currentStatus;
        this.PreviousWaitMinutes = previousWaitMinutes;
        this.CurrentWaitMinutes = currentWaitMinutes;
        this.SourceId = sourceId;
        this.ObservedAtUtc = observedAtUtc;
        this.DeliveredAtUtc = deliveredAtUtc;
        this.AgeSeconds = ageSeconds;
        this.Status = status;
        this.ReadAtUtc = readAtUtc;
        this.DismissedAtUtc = dismissedAtUtc;
        this.ExpiresAtUtc = expiresAtUtc;
        this.Version = version;
    }

    public LiveAlertNotificationId Id { get; }
    public string UserId { get; }
    public LiveAlertSubscriptionId SubscriptionId { get; }
    public string TargetId { get; }
    public string ParkId { get; }
    public LiveAlertType Type { get; }
    public int? ThresholdMinutes { get; }
    public LiveOperationalStatus? PreviousStatus { get; }
    public LiveOperationalStatus CurrentStatus { get; }
    public int? PreviousWaitMinutes { get; }
    public int? CurrentWaitMinutes { get; }
    public LiveDataSourceId SourceId { get; }
    public DateTime ObservedAtUtc { get; }
    public DateTime DeliveredAtUtc { get; }
    public long AgeSeconds { get; }
    public UserNotificationStatus Status { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public DateTime? DismissedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; }
    public long Version { get; private set; }

    public static LiveAlertNotification Create(
        LiveAlertNotificationId id,
        LiveAlertSubscription subscription,
        LiveAlertTrigger trigger)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(trigger);
        return new LiveAlertNotification(
            id,
            subscription.UserId,
            subscription.Id,
            subscription.TargetId,
            subscription.ParkId,
            trigger.Type,
            trigger.ThresholdMinutes,
            trigger.PreviousStatus,
            trigger.CurrentStatus,
            trigger.PreviousWaitMinutes,
            trigger.CurrentWaitMinutes,
            trigger.SourceId,
            trigger.ObservedAtUtc,
            trigger.TriggeredAtUtc,
            trigger.AgeSeconds,
            UserNotificationStatus.Delivered,
            null,
            null,
            trigger.TriggeredAtUtc.AddDays(RetentionDays),
            1);
    }

    public static LiveAlertNotification Restore(
        LiveAlertNotificationId id,
        string userId,
        LiveAlertSubscriptionId subscriptionId,
        string targetId,
        string parkId,
        LiveAlertType type,
        int? thresholdMinutes,
        LiveOperationalStatus? previousStatus,
        LiveOperationalStatus currentStatus,
        int? previousWaitMinutes,
        int? currentWaitMinutes,
        LiveDataSourceId sourceId,
        DateTime observedAtUtc,
        DateTime deliveredAtUtc,
        long ageSeconds,
        UserNotificationStatus status,
        DateTime? readAtUtc,
        DateTime? dismissedAtUtc,
        DateTime expiresAtUtc,
        long version)
    {
        return new LiveAlertNotification(
            id, userId, subscriptionId, targetId, parkId, type, thresholdMinutes,
            previousStatus, currentStatus, previousWaitMinutes, currentWaitMinutes,
            sourceId, observedAtUtc, deliveredAtUtc, ageSeconds, status, readAtUtc,
            dismissedAtUtc, expiresAtUtc, version);
    }

    public void MarkRead(DateTime nowUtc)
    {
        EnsureUtc(nowUtc, nameof(nowUtc));
        if (this.Status != UserNotificationStatus.Delivered)
        {
            return;
        }

        this.Status = UserNotificationStatus.Read;
        this.ReadAtUtc = nowUtc;
        this.Version++;
    }

    public void Dismiss(DateTime nowUtc)
    {
        EnsureUtc(nowUtc, nameof(nowUtc));
        if (this.Status == UserNotificationStatus.Dismissed)
        {
            return;
        }

        this.Status = UserNotificationStatus.Dismissed;
        this.DismissedAtUtc = nowUtc;
        this.Version++;
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Live alert timestamps must use UTC.", parameterName);
        }
    }
}
