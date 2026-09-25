using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

[BsonIgnoreExtraElements]
public sealed class TripActivityEventDocument : MongoDocumentBase
{
    [BsonElement("tripPlanId")]
    public string TripPlanId { get; set; } = string.Empty;

    [BsonElement("actorMemberId")]
    [BsonIgnoreIfNull]
    public string? ActorMemberId { get; set; }

    [BsonElement("actorRole")]
    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public TripEffectiveRole? ActorRole { get; set; }

    [BsonElement("kind")]
    [BsonRepresentation(BsonType.String)]
    public TripActivityKind Kind { get; set; }

    [BsonElement("operationKey")]
    public string OperationKey { get; set; } = string.Empty;

    [BsonElement("sequence")]
    public long Sequence { get; set; }

    [BsonElement("affectedCount")]
    public int AffectedCount { get; set; }

}
