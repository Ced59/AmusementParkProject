using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

[BsonIgnoreExtraElements]
public sealed class TripDayPlanDocument : MongoDocumentBase
{
    [BsonElement("tripPlanId")]
    public string TripPlanId { get; set; } = string.Empty;

    [BsonElement("localDate")]
    public string LocalDate { get; set; } = string.Empty;

    [BsonElement("parkCandidateId")]
    public string ParkCandidateId { get; set; } = string.Empty;

    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("desiredArrivalTime")]
    [BsonIgnoreIfNull]
    public string? DesiredArrivalTime { get; set; }

    [BsonElement("groupNote")]
    [BsonIgnoreIfNull]
    public string? GroupNote { get; set; }

    [BsonElement("blocks")]
    public List<TripDayBlockDocument> Blocks { get; set; } = new();

    [BsonElement("version")]
    public long Version { get; set; }

    [BsonElement("documentState")]
    [BsonRepresentation(BsonType.String)]
    public TripChildDocumentState DocumentState { get; set; }

    [BsonElement("operationId")]
    public string OperationId { get; set; } = string.Empty;

    [BsonElement("requestHash")]
    public string RequestHash { get; set; } = string.Empty;

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
}
