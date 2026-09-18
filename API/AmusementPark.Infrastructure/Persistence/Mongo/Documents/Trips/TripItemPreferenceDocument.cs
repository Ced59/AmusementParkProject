using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

[BsonIgnoreExtraElements]
public sealed class TripItemPreferenceDocument : MongoDocumentBase
{
    [BsonElement("tripPlanId")]
    public string TripPlanId { get; set; } = string.Empty;

    [BsonElement("memberId")]
    public string MemberId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("parkItemId")]
    public string ParkItemId { get; set; } = string.Empty;

    [BsonElement("level")]
    [BsonRepresentation(BsonType.String)]
    public TripItemPreferenceLevel Level { get; set; }

    [BsonElement("reason")]
    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public TripItemPreferenceReason? Reason { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }

    [BsonElement("documentState")]
    [BsonRepresentation(BsonType.String)]
    public TripChildDocumentState DocumentState { get; set; }

    [BsonElement("operationId")]
    public string OperationId { get; set; } = string.Empty;

    [BsonElement("childMutationEpoch")]
    public long ChildMutationEpoch { get; set; }

    [BsonElement("leaseGeneration")]
    public long LeaseGeneration { get; set; }

    [BsonElement("leaseExpiresAtUtc")]
    public DateTime LeaseExpiresAtUtc { get; set; }

    [BsonElement("reservedExpiresAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? ReservedExpiresAtUtc { get; set; }

    [BsonElement("pendingMutation")]
    [BsonIgnoreIfNull]
    public PendingTripChildMutationDocument? PendingMutation { get; set; }

    [BsonElement("pendingAuditEvents")]
    public List<TripActivityPendingDocument> PendingAuditEvents { get; set; } = new();
}
