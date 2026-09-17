using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

[BsonIgnoreExtraElements]
public sealed class TripParkCandidateDocument : MongoDocumentBase
{
    [BsonElement("tripPlanId")]
    public string TripPlanId { get; set; } = string.Empty;

    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("candidateDates")]
    public List<string> CandidateDates { get; set; } = new();

    [BsonElement("source")]
    [BsonRepresentation(BsonType.String)]
    public TripParkCandidateSource Source { get; set; }

    [BsonElement("candidateState")]
    [BsonRepresentation(BsonType.String)]
    public TripParkCandidateState CandidateState { get; set; }

    [BsonElement("collectiveNote")]
    [BsonIgnoreIfNull]
    public string? CollectiveNote { get; set; }

    [BsonElement("fitSnapshot")]
    [BsonIgnoreIfNull]
    public TripFitRecommendationSnapshotDocument? FitSnapshot { get; set; }

    [BsonElement("addedByMemberId")]
    public string AddedByMemberId { get; set; } = string.Empty;

    [BsonElement("sortPosition")]
    public long SortPosition { get; set; }

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

    [BsonElement("tombstoneExpiresAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? TombstoneExpiresAtUtc { get; set; }

    [BsonElement("pendingMutation")]
    [BsonIgnoreIfNull]
    public PendingTripChildMutationDocument? PendingMutation { get; set; }
}
