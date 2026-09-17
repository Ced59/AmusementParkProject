using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

[BsonIgnoreExtraElements]
public sealed class PendingTripChildMutationDocument
{
    [BsonElement("operationId")]
    public string OperationId { get; set; } = string.Empty;

    [BsonElement("childMutationEpoch")]
    public long ChildMutationEpoch { get; set; }

    [BsonElement("leaseGeneration")]
    public long LeaseGeneration { get; set; }

    [BsonElement("leaseExpiresAtUtc")]
    public DateTime LeaseExpiresAtUtc { get; set; }
}
