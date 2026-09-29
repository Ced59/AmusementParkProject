using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

[BsonIgnoreExtraElements]
public sealed class LiveAlertSubscriptionDocument : MongoDocumentBase
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("targetId")]
    public string TargetId { get; set; } = string.Empty;

    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("type")]
    public LiveAlertType Type { get; set; }

    [BsonElement("thresholdMinutes")]
    [BsonIgnoreIfNull]
    public int? ThresholdMinutes { get; set; }

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("lastObservedAt")]
    [BsonIgnoreIfNull]
    public DateTime? LastObservedAt { get; set; }

    [BsonElement("lastStatus")]
    [BsonIgnoreIfNull]
    public LiveOperationalStatus? LastStatus { get; set; }

    [BsonElement("lastWaitMinutes")]
    [BsonIgnoreIfNull]
    public int? LastWaitMinutes { get; set; }

    [BsonElement("isArmed")]
    public bool IsArmed { get; set; }

    [BsonElement("lastTriggeredAt")]
    [BsonIgnoreIfNull]
    public DateTime? LastTriggeredAt { get; set; }

    [BsonElement("pendingTrigger")]
    [BsonIgnoreIfNull]
    public LiveAlertTriggerDocument? PendingTrigger { get; set; }

    [BsonElement("quotaSlot")]
    public int QuotaSlot { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }
}
