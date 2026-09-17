using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

[BsonIgnoreExtraElements]
public sealed class NotificationDeliveryAttemptDocument : MongoDocumentBase
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("digestId")]
    public string DigestId { get; set; } = string.Empty;

    [BsonElement("status")]
    public NotificationDeliveryAttemptStatus Status { get; set; }

    [BsonElement("attemptCount")]
    public int AttemptCount { get; set; }

    [BsonElement("lastErrorCode")]
    [BsonIgnoreIfNull]
    public string? LastErrorCode { get; set; }

    [BsonElement("completedAt")]
    [BsonIgnoreIfNull]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }
}
