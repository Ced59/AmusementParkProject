using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Watchlists;

public sealed record NotificationDigestEntry
{
    public NotificationDigestEntry(
        FactualChangeEventId factualEventId,
        WatchSubscriptionId subscriptionId,
        string deduplicationKey,
        long sourceRevision,
        FactualEventType eventType,
        FactualTargetType targetType,
        string targetId,
        FactualChangeStatus sourceStatus,
        DateTime occurredAtUtc)
    {
        _ = factualEventId.Value;
        _ = subscriptionId.Value;
        this.DeduplicationKey = IdentifierRules.NormalizeRequired(
            deduplicationKey,
            nameof(deduplicationKey));
        if (sourceRevision < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceRevision));
        }

        if (!Enum.IsDefined(eventType) || !Enum.IsDefined(targetType) || !Enum.IsDefined(sourceStatus))
        {
            throw new ArgumentException("The digest entry fact is invalid.");
        }

        if (occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The digest entry timestamp must use UTC.", nameof(occurredAtUtc));
        }

        this.FactualEventId = factualEventId;
        this.SubscriptionId = subscriptionId;
        this.SourceRevision = sourceRevision;
        this.EventType = eventType;
        this.TargetType = targetType;
        this.TargetId = IdentifierRules.NormalizeRequired(targetId, nameof(targetId));
        this.SourceStatus = sourceStatus;
        this.OccurredAtUtc = occurredAtUtc;
    }

    public FactualChangeEventId FactualEventId { get; }

    public WatchSubscriptionId SubscriptionId { get; }

    public string DeduplicationKey { get; }

    public long SourceRevision { get; }

    public FactualEventType EventType { get; }

    public FactualTargetType TargetType { get; }

    public string TargetId { get; }

    public FactualChangeStatus SourceStatus { get; }

    public DateTime OccurredAtUtc { get; }
}
