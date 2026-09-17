using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

[BsonIgnoreExtraElements]
public sealed class TripChildMutationLeaseDocument
{
    [BsonElement("operationId")]
    public string OperationId { get; set; } = string.Empty;

    [BsonElement("actorMemberId")]
    public string ActorMemberId { get; set; } = string.Empty;

    [BsonElement("childMutationEpoch")]
    public long ChildMutationEpoch { get; set; }

    [BsonElement("generation")]
    public long Generation { get; set; }

    [BsonElement("expiresAtUtc")]
    public DateTime ExpiresAtUtc { get; set; }
}
