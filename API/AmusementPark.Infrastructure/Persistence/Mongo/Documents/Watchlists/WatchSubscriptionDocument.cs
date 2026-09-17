using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

[BsonIgnoreExtraElements]
public sealed class WatchSubscriptionDocument : MongoDocumentBase
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("targetType")]
    public CollectionTargetType TargetType { get; set; }

    [BsonElement("targetId")]
    public string TargetId { get; set; } = string.Empty;

    [BsonElement("eventTypes")]
    public List<FactualEventType> EventTypes { get; set; } = new();

    [BsonElement("frequency")]
    public NotificationFrequency Frequency { get; set; }

    [BsonElement("channels")]
    public List<NotificationChannel> Channels { get; set; } = new();

    [BsonElement("isPaused")]
    public bool IsPaused { get; set; }

    [BsonElement("ownerSlot")]
    public int OwnerSlot { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }
}
