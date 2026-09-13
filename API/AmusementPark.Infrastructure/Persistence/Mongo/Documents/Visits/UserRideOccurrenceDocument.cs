using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

[BsonIgnoreExtraElements]
public sealed class UserRideOccurrenceDocument : MongoDocumentBase
{
    [BsonElement("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [BsonElement("visitId")]
    public string VisitId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("parkItemId")]
    public string ParkItemId { get; set; } = string.Empty;

    [BsonElement("sortPosition")]
    public long SortPosition { get; set; }

    [BsonElement("moment")]
    public RideOccurrenceMomentDocument Moment { get; set; } = new RideOccurrenceMomentDocument();

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public RideOccurrenceStatus Status { get; set; }

    [BsonElement("source")]
    [BsonRepresentation(BsonType.String)]
    public RideLogSource Source { get; set; }

    [BsonElement("historicalConsistency")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalConsistency HistoricalConsistency { get; set; }

    [BsonElement("historicalTarget")]
    [BsonIgnoreIfNull]
    public HistoricalTargetReferenceDocument? HistoricalTarget { get; set; }

    [BsonElement("privateNote")]
    [BsonIgnoreIfNull]
    public string? PrivateNote { get; set; }

    [BsonElement("assessment")]
    [BsonIgnoreIfNull]
    public UserRideAssessmentDocument? Assessment { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }

    [BsonElement("deletedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? DeletedAtUtc { get; set; }

    [BsonElement("creationOperationKeyHash")]
    [BsonIgnoreIfNull]
    public string? CreationOperationKeyHash { get; set; }

    [BsonElement("creationPayloadHash")]
    [BsonIgnoreIfNull]
    public string? CreationPayloadHash { get; set; }

    [BsonElement("creationOperationIndex")]
    [BsonIgnoreIfNull]
    public int? CreationOperationIndex { get; set; }

    [BsonElement("creationOperationCount")]
    [BsonIgnoreIfNull]
    public int? CreationOperationCount { get; set; }

    [BsonElement("creationSnapshot")]
    [BsonIgnoreIfNull]
    public UserRideOccurrenceCreationSnapshotDocument? CreationSnapshot { get; set; }

    [BsonElement("creationPendingCompletion")]
    [BsonIgnoreIfNull]
    public bool? CreationPendingCompletion { get; set; }

    [BsonElement("lastReorderOperationKeyHash")]
    [BsonIgnoreIfNull]
    public string? LastReorderOperationKeyHash { get; set; }

    [BsonElement("lastDeleteOperationKeyHash")]
    [BsonIgnoreIfNull]
    public string? LastDeleteOperationKeyHash { get; set; }

    [BsonElement("pendingAuditEvents")]
    [BsonIgnoreIfNull]
    public List<PassportAuditEventDocument>? PendingAuditEvents { get; set; }

    [BsonElement("contentMutationFenceToken")]
    [BsonIgnoreIfNull]
    public long? ContentMutationFenceToken { get; set; }
}
