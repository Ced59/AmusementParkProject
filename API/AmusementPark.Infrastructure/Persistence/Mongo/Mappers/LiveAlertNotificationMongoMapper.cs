using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class LiveAlertNotificationMongoMapper
{
    public static LiveAlertNotificationDocument ToDocument(this LiveAlertNotification notification)
    {
        return new LiveAlertNotificationDocument
        {
            Id = notification.Id.Value,
            TriggerKey = $"{notification.SubscriptionId.Value}:{notification.ObservedAtUtc.Ticks}",
            UserId = notification.UserId,
            SubscriptionId = notification.SubscriptionId.Value,
            TargetId = notification.TargetId,
            ParkId = notification.ParkId,
            Type = notification.Type,
            ThresholdMinutes = notification.ThresholdMinutes,
            PreviousStatus = notification.PreviousStatus,
            CurrentStatus = notification.CurrentStatus,
            PreviousWaitMinutes = notification.PreviousWaitMinutes,
            CurrentWaitMinutes = notification.CurrentWaitMinutes,
            SourceId = notification.SourceId.Value,
            ObservedAt = notification.ObservedAtUtc,
            DeliveredAt = notification.DeliveredAtUtc,
            AgeSeconds = notification.AgeSeconds,
            Status = notification.Status,
            ReadAt = notification.ReadAtUtc,
            DismissedAt = notification.DismissedAtUtc,
            ExpiresAt = notification.ExpiresAtUtc,
            Version = notification.Version,
            CreatedAt = notification.DeliveredAtUtc,
            UpdatedAt = notification.DismissedAtUtc ?? notification.ReadAtUtc ?? notification.DeliveredAtUtc,
        };
    }

    public static LiveAlertNotification ToDomain(this LiveAlertNotificationDocument document)
    {
        return LiveAlertNotification.Restore(
            LiveAlertNotificationId.Parse(document.Id),
            document.UserId,
            LiveAlertSubscriptionId.Parse(document.SubscriptionId),
            document.TargetId,
            document.ParkId,
            document.Type,
            document.ThresholdMinutes,
            document.PreviousStatus,
            document.CurrentStatus,
            document.PreviousWaitMinutes,
            document.CurrentWaitMinutes,
            LiveDataSourceId.Parse(document.SourceId),
            document.ObservedAt,
            document.DeliveredAt,
            document.AgeSeconds,
            document.Status,
            document.ReadAt,
            document.DismissedAt,
            document.ExpiresAt,
            document.Version);
    }
}
