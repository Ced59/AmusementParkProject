using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

[BsonIgnoreExtraElements]
public sealed class HistoricalReviewEventDocument : MongoDocumentBase
{
    [BsonElement("resourceType")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalReviewResourceType ResourceType { get; set; }

    [BsonElement("resourceId")]
    public string ResourceId { get; set; } = string.Empty;

    [BsonElement("resourceRevision")]
    public int ResourceRevision { get; set; }

    [BsonElement("eventType")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalReviewEventType EventType { get; set; }

    [BsonElement("actorUserId")]
    public string ActorUserId { get; set; } = string.Empty;

    [BsonElement("privateNote")]
    [BsonIgnoreIfNull]
    public string? PrivateNote { get; set; }

    [BsonElement("occurredAtUtc")]
    public DateTime OccurredAtUtc { get; set; }
}
