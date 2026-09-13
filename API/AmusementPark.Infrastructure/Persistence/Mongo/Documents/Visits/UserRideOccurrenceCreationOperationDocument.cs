using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

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
