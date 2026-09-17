using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class NotificationDigestMongoMapper
{
    public static NotificationDigestDocument ToDocument(this NotificationDigest digest)
    {
        ArgumentNullException.ThrowIfNull(digest);
        return new NotificationDigestDocument
        {
            Id = digest.Id.Value,
            UserId = digest.UserId,
            Channel = digest.Channel,
            Frequency = digest.Frequency,
            PeriodStart = digest.PeriodStartUtc,
            PeriodEnd = digest.PeriodEndUtc,
            Entries = digest.Entries.Select(ToDocument).ToList(),
            ObservedNotificationCount = digest.ObservedNotificationCount,
            CreatedAt = digest.CreatedAtUtc,
            UpdatedAt = digest.UpdatedAtUtc,
        };
    }

    public static NotificationDigest ToDomain(this NotificationDigestDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return NotificationDigest.Restore(
            NotificationDigestId.Parse(document.Id),
            document.UserId,
            document.Channel,
            document.Frequency,
            document.PeriodStart,
            document.PeriodEnd,
            document.Entries.Select(ToDomain).ToArray(),
            document.ObservedNotificationCount,
            document.CreatedAt,
            document.UpdatedAt);
    }

    private static NotificationDigestEntryDocument ToDocument(NotificationDigestEntry entry)
    {
        return new NotificationDigestEntryDocument
        {
            FactualEventId = entry.FactualEventId.Value,
            SubscriptionId = entry.SubscriptionId.Value,
            DeduplicationKey = entry.DeduplicationKey,
            SourceRevision = entry.SourceRevision,
            EventType = entry.EventType,
            TargetType = entry.TargetType,
            TargetId = entry.TargetId,
            SourceStatus = entry.SourceStatus,
            OccurredAt = entry.OccurredAtUtc,
        };
    }

    private static NotificationDigestEntry ToDomain(NotificationDigestEntryDocument document)
    {
        return new NotificationDigestEntry(
            FactualChangeEventId.Parse(document.FactualEventId),
            WatchSubscriptionId.Parse(document.SubscriptionId),
            document.DeduplicationKey,
            document.SourceRevision,
            document.EventType,
            document.TargetType,
            document.TargetId,
            document.SourceStatus,
            document.OccurredAt);
    }
}
