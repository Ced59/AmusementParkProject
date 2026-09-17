using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

[BsonIgnoreExtraElements]
public sealed class UserNotificationDocument : MongoDocumentBase
{
    public const string MisleadingReportedAtFieldName = "misleadingReportedAt";

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("factualEventId")]
    public string FactualEventId { get; set; } = string.Empty;

    [BsonElement("subscriptionId")]
    public string SubscriptionId { get; set; } = string.Empty;

    [BsonElement("eventType")]
    public FactualEventType EventType { get; set; }

    [BsonElement("targetType")]
    public FactualTargetType TargetType { get; set; }

    [BsonElement("targetId")]
    public string TargetId { get; set; } = string.Empty;

    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("sourceRevision")]
    public long SourceRevision { get; set; }

    [BsonElement("templateVersion")]
    public int TemplateVersion { get; set; }

    [BsonElement("language")]
    public string Language { get; set; } = string.Empty;

    [BsonElement("status")]
    public UserNotificationStatus Status { get; set; }

    [BsonElement("deliveredAt")]
    public DateTime DeliveredAt { get; set; }

    [BsonElement("readAt")]
    [BsonIgnoreIfNull]
    public DateTime? ReadAt { get; set; }

    [BsonElement("dismissedAt")]
    [BsonIgnoreIfNull]
    public DateTime? DismissedAt { get; set; }

    [BsonElement(MisleadingReportedAtFieldName)]
    [BsonIgnoreIfNull]
    public DateTime? MisleadingReportedAt { get; set; }

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }
}
