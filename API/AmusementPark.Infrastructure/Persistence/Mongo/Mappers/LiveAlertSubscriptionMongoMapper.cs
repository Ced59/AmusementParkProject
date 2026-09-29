using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class LiveAlertSubscriptionMongoMapper
{
    public static LiveAlertSubscriptionDocument ToDocument(this LiveAlertSubscription subscription)
    {
        return new LiveAlertSubscriptionDocument
        {
            Id = subscription.Id.Value,
            UserId = subscription.UserId,
            TargetId = subscription.TargetId,
            ParkId = subscription.ParkId,
            Type = subscription.Type,
            ThresholdMinutes = subscription.ThresholdMinutes,
            CreatedAt = subscription.CreatedAtUtc,
            UpdatedAt = subscription.LastObservedAtUtc ?? subscription.CreatedAtUtc,
            ExpiresAt = subscription.ExpiresAtUtc,
            RetentionExpiresAt = subscription.RetentionExpiresAtUtc,
            LastObservedAt = subscription.LastObservedAtUtc,
            LastStatus = subscription.LastStatus,
            LastWaitMinutes = subscription.LastWaitMinutes,
            IsArmed = subscription.IsArmed,
            LastTriggeredAt = subscription.LastTriggeredAtUtc,
            PendingTrigger = subscription.PendingTrigger?.ToDocument(),
            Version = subscription.Version,
        };
    }

    public static LiveAlertSubscription ToDomain(this LiveAlertSubscriptionDocument document)
    {
        return LiveAlertSubscription.Restore(
            LiveAlertSubscriptionId.Parse(document.Id),
            document.UserId,
            document.TargetId,
            document.ParkId,
            document.Type,
            document.ThresholdMinutes,
            document.CreatedAt,
            document.ExpiresAt,
            document.LastObservedAt,
            document.LastStatus,
            document.LastWaitMinutes,
            document.IsArmed,
            document.LastTriggeredAt,
            document.PendingTrigger?.ToDomain(),
            document.Version);
    }

    private static LiveAlertTriggerDocument ToDocument(this LiveAlertTrigger trigger)
    {
        return new LiveAlertTriggerDocument
        {
            Type = trigger.Type,
            PreviousStatus = trigger.PreviousStatus,
            CurrentStatus = trigger.CurrentStatus,
            PreviousWaitMinutes = trigger.PreviousWaitMinutes,
            CurrentWaitMinutes = trigger.CurrentWaitMinutes,
            ThresholdMinutes = trigger.ThresholdMinutes,
            SourceId = trigger.SourceId.Value,
            ObservedAt = trigger.ObservedAtUtc,
            TriggeredAt = trigger.TriggeredAtUtc,
            AgeSeconds = trigger.AgeSeconds,
        };
    }

    private static LiveAlertTrigger ToDomain(this LiveAlertTriggerDocument document)
    {
        return new LiveAlertTrigger(
            document.Type,
            document.PreviousStatus,
            document.CurrentStatus,
            document.PreviousWaitMinutes,
            document.CurrentWaitMinutes,
            document.ThresholdMinutes,
            LiveDataSourceId.Parse(document.SourceId),
            document.ObservedAt,
            document.TriggeredAt,
            document.AgeSeconds);
    }
}
