using AmusementPark.Core.Domain.FactualEvents;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

[BsonIgnoreExtraElements]
public sealed class NotificationDigestEntryDocument
{
    [BsonElement("factualEventId")]
    public string FactualEventId { get; set; } = string.Empty;

    [BsonElement("subscriptionId")]
    public string SubscriptionId { get; set; } = string.Empty;

    [BsonElement("deduplicationKey")]
    public string DeduplicationKey { get; set; } = string.Empty;

    [BsonElement("sourceRevision")]
    public long SourceRevision { get; set; }

    [BsonElement("eventType")]
    public FactualEventType EventType { get; set; }

    [BsonElement("targetType")]
    public FactualTargetType TargetType { get; set; }

    [BsonElement("targetId")]
    public string TargetId { get; set; } = string.Empty;

    [BsonElement("sourceStatus")]
    public FactualChangeStatus SourceStatus { get; set; }

    [BsonElement("occurredAt")]
    public DateTime OccurredAt { get; set; }
}
