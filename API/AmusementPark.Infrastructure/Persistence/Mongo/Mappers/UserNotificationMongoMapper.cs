using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class UserNotificationMongoMapper
{
    public static UserNotificationDocument ToDocument(this UserNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        DateTime[] mutationTimestamps =
        [
            notification.DeliveredAtUtc,
            notification.ReadAtUtc ?? notification.DeliveredAtUtc,
            notification.DismissedAtUtc ?? notification.DeliveredAtUtc,
            notification.MisleadingReportedAtUtc ?? notification.DeliveredAtUtc,
        ];
        return new UserNotificationDocument
        {
            Id = notification.Id.Value,
            UserId = notification.UserId,
            FactualEventId = notification.FactualEventId.Value,
            SubscriptionId = notification.SubscriptionId.Value,
            EventType = notification.EventType,
            TargetType = notification.TargetType,
            TargetId = notification.TargetId,
            ParkId = notification.ParkId,
            SourceRevision = notification.SourceRevision,
            TemplateVersion = notification.TemplateVersion,
            Language = notification.Language,
            Status = notification.Status,
            DeliveredAt = notification.DeliveredAtUtc,
            ReadAt = notification.ReadAtUtc,
            DismissedAt = notification.DismissedAtUtc,
            MisleadingReportedAt = notification.MisleadingReportedAtUtc,
            ExpiresAt = notification.ExpiresAtUtc,
            CreatedAt = notification.CreatedAtUtc,
            UpdatedAt = mutationTimestamps.Max(),
            Version = notification.Version,
        };
    }

    public static UserNotification ToDomain(this UserNotificationDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return UserNotification.Restore(
            UserNotificationId.Parse(document.Id),
            document.UserId,
            FactualChangeEventId.Parse(document.FactualEventId),
            WatchSubscriptionId.Parse(document.SubscriptionId),
            document.EventType,
            document.TargetType,
            document.TargetId,
            document.ParkId,
            document.SourceRevision,
            document.TemplateVersion,
            document.Language,
            document.Status,
            document.CreatedAt,
            document.DeliveredAt,
            document.ReadAt,
            document.DismissedAt,
            document.ExpiresAt,
            document.Version,
            document.MisleadingReportedAt);
    }
}
