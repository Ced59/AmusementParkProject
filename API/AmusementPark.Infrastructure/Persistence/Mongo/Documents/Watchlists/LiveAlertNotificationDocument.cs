using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

[BsonIgnoreExtraElements]
public sealed class LiveAlertNotificationDocument : MongoDocumentBase
{
    [BsonElement("triggerKey")]
    public string TriggerKey { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("subscriptionId")]
    public string SubscriptionId { get; set; } = string.Empty;

    [BsonElement("targetId")]
    public string TargetId { get; set; } = string.Empty;

    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("type")]
    public LiveAlertType Type { get; set; }

    [BsonElement("thresholdMinutes")]
    [BsonIgnoreIfNull]
    public int? ThresholdMinutes { get; set; }

    [BsonElement("previousStatus")]
    [BsonIgnoreIfNull]
    public LiveOperationalStatus? PreviousStatus { get; set; }

    [BsonElement("currentStatus")]
    public LiveOperationalStatus CurrentStatus { get; set; }

    [BsonElement("previousWaitMinutes")]
    [BsonIgnoreIfNull]
    public int? PreviousWaitMinutes { get; set; }

    [BsonElement("currentWaitMinutes")]
    [BsonIgnoreIfNull]
    public int? CurrentWaitMinutes { get; set; }

    [BsonElement("sourceId")]
    public string SourceId { get; set; } = string.Empty;

    [BsonElement("observedAt")]
    public DateTime ObservedAt { get; set; }

    [BsonElement("deliveredAt")]
    public DateTime DeliveredAt { get; set; }

    [BsonElement("ageSeconds")]
    public long AgeSeconds { get; set; }

    [BsonElement("status")]
    public UserNotificationStatus Status { get; set; }

    [BsonElement("readAt")]
    [BsonIgnoreIfNull]
    public DateTime? ReadAt { get; set; }

    [BsonElement("dismissedAt")]
    [BsonIgnoreIfNull]
    public DateTime? DismissedAt { get; set; }

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }
}
