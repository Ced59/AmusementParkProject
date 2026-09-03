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

[BsonIgnoreExtraElements]
public sealed class RideOccurrenceMomentDocument
{
    [BsonElement("localTime")]
    [BsonIgnoreIfNull]
    public string? LocalTime { get; set; }

    [BsonElement("isApproximate")]
    public bool IsApproximate { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class HistoricalTargetReferenceDocument
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("category")]
    [BsonIgnoreIfNull]
    public string? Category { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class UserRideOccurrenceCreationSnapshotDocument
{
    [BsonElement("visitId")]
    public string VisitId { get; set; } = string.Empty;

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

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class UserRideAssessmentDocument
{
    [BsonElement("valueHalfSteps")]
    public byte ValueHalfSteps { get; set; }

    [BsonElement("privateComment")]
    [BsonIgnoreIfNull]
    public string? PrivateComment { get; set; }

    [BsonElement("revision")]
    public int Revision { get; set; }

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class UserRideOccurrenceCreationOperationDocument : MongoDocumentBase
{
    [BsonElement("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("operationKeyHash")]
    public string OperationKeyHash { get; set; } = string.Empty;

    [BsonElement("payloadHash")]
    public string PayloadHash { get; set; } = string.Empty;

    [BsonElement("operationKind")]
    public string OperationKind { get; set; } = "creation";

    [BsonElement("visitId")]
    [BsonIgnoreIfNull]
    public string? VisitId { get; set; }

    [BsonElement("contentMutationFenceToken")]
    [BsonIgnoreIfNull]
    public long? ContentMutationFenceToken { get; set; }

    [BsonElement("operationState")]
    [BsonIgnoreIfNull]
    public string? OperationState { get; set; }

    [BsonElement("creationPreparation")]
    [BsonIgnoreIfNull]
    public UserRideOccurrenceCreationPreparationDocument? CreationPreparation { get; set; }

    [BsonElement("appendBaseWasEmpty")]
    [BsonIgnoreIfDefault]
    public bool AppendBaseWasEmpty { get; set; }

    [BsonElement("appendBaseSortPosition")]
    [BsonIgnoreIfNull]
    public long? AppendBaseSortPosition { get; set; }

    [BsonElement("appendBaseValidated")]
    [BsonIgnoreIfDefault]
    public bool AppendBaseValidated { get; set; }

    [BsonElement("movedOccurrenceId")]
    [BsonIgnoreIfNull]
    public string? MovedOccurrenceId { get; set; }

    [BsonElement("reorderExpectedVersion")]
    [BsonIgnoreIfNull]
    public long? ReorderExpectedVersion { get; set; }

    [BsonElement("reorderAnchorOccurrenceId")]
    [BsonIgnoreIfNull]
    public string? ReorderAnchorOccurrenceId { get; set; }

    [BsonElement("reorderPlacement")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.String)]
    public RideOccurrencePlacement? ReorderPlacement { get; set; }

    [BsonElement("deleteOccurrenceId")]
    [BsonIgnoreIfNull]
    public string? DeleteOccurrenceId { get; set; }

    [BsonElement("deleteExpectedVersion")]
    [BsonIgnoreIfNull]
    public long? DeleteExpectedVersion { get; set; }

    [BsonElement("deleteAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? DeleteAtUtc { get; set; }

    [BsonElement("wasNormalized")]
    [BsonIgnoreIfDefault]
    public bool WasNormalized { get; set; }

    [BsonElement("relatedCreationOperationKeyHash")]
    [BsonIgnoreIfNull]
    public string? RelatedCreationOperationKeyHash { get; set; }

    [BsonElement("items")]
    public List<UserRideOccurrenceCreationAllocationDocument> Items { get; set; } =
        new List<UserRideOccurrenceCreationAllocationDocument>();

    [BsonElement("reorderItems")]
    [BsonIgnoreIfNull]
    public List<UserRideOccurrenceReorderAllocationDocument>? ReorderItems { get; set; }

    [BsonElement("orderGuards")]
    [BsonIgnoreIfNull]
    public List<UserRideOccurrenceOrderGuardDocument>? OrderGuards { get; set; }

    [BsonElement("orderGuardsValidated")]
    [BsonIgnoreIfDefault]
    public bool OrderGuardsValidated { get; set; }

    [BsonElement("reorderCompensationStarted")]
    [BsonIgnoreIfDefault]
    public bool ReorderCompensationStarted { get; set; }

    [BsonElement("reorderResultSnapshot")]
    [BsonIgnoreIfNull]
    public UserRideOccurrenceCreationSnapshotDocument? ReorderResultSnapshot { get; set; }

    [BsonElement("pendingAuditEvents")]
    [BsonIgnoreIfNull]
    public List<PassportAuditEventDocument>? PendingAuditEvents { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class UserRideOccurrenceCreationPreparationDocument
{
    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("visitDate")]
    public VisitDateDocument VisitDate { get; set; } = new VisitDateDocument();

    [BsonElement("timeZoneId")]
    [BsonIgnoreIfNull]
    public string? TimeZoneId { get; set; }

    [BsonElement("serviceDayConvention")]
    [BsonRepresentation(BsonType.String)]
    public LocalServiceDayConvention ServiceDayConvention { get; set; }

    [BsonElement("items")]
    public List<UserRideOccurrenceCreationPreparationItemDocument> Items { get; set; } =
        new List<UserRideOccurrenceCreationPreparationItemDocument>();
}

[BsonIgnoreExtraElements]
public sealed class UserRideOccurrenceCreationPreparationItemDocument
{
    [BsonElement("index")]
    public int Index { get; set; }

    [BsonElement("historicalConsistency")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalConsistency HistoricalConsistency { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class UserRideOccurrenceCreationAllocationDocument
{
    [BsonElement("index")]
    public int Index { get; set; }

    [BsonElement("occurrenceId")]
    public string OccurrenceId { get; set; } = string.Empty;

    [BsonElement("sortPosition")]
    public long SortPosition { get; set; }

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }

    [BsonElement("creationSnapshot")]
    public UserRideOccurrenceCreationSnapshotDocument CreationSnapshot { get; set; } =
        new UserRideOccurrenceCreationSnapshotDocument();
}

[BsonIgnoreExtraElements]
public sealed class UserRideOccurrenceOrderGuardDocument
{
    [BsonElement("occurrenceId")]
    public string OccurrenceId { get; set; } = string.Empty;

    [BsonElement("sortPosition")]
    public long SortPosition { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class UserRideOccurrenceReorderAllocationDocument
{
    [BsonElement("index")]
    public int Index { get; set; }

    [BsonElement("occurrenceId")]
    public string OccurrenceId { get; set; } = string.Empty;

    [BsonElement("expectedVersion")]
    public long ExpectedVersion { get; set; }

    [BsonElement("previousSortPosition")]
    public long PreviousSortPosition { get; set; }

    [BsonElement("resultSortPosition")]
    public long ResultSortPosition { get; set; }

    [BsonElement("resultVersion")]
    public long ResultVersion { get; set; }

    [BsonElement("resultUpdatedAtUtc")]
    public DateTime ResultUpdatedAtUtc { get; set; }
}
