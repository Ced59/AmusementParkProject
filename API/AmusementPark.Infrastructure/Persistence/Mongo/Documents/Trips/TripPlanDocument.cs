using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

[BsonIgnoreExtraElements]
public sealed class TripPlanDocument : MongoDocumentBase
{
    [BsonElement("ownerUserId")]
    public string OwnerUserId { get; set; } = string.Empty;

    [BsonElement("ownerScopeHash")]
    public string OwnerScopeHash { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("dateProposal")]
    public TripDateProposalDocument DateProposal { get; set; } = new();

    [BsonElement("destinationTimeZoneId")]
    [BsonIgnoreIfNull]
    public string? DestinationTimeZoneId { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public TripPlanStatus Status { get; set; }

    [BsonElement("accessScope")]
    [BsonRepresentation(BsonType.String)]
    public TripPlanAccessScope AccessScope { get; set; }

    [BsonElement("members")]
    public List<TripMemberDocument> Members { get; set; } = new();

    [BsonElement("departedPreferenceCleanupUserIds")]
    public List<string> DepartedPreferenceCleanupUserIds { get; set; } = new();

    [BsonElement("memberAdmissionFence")]
    [BsonIgnoreIfNull]
    public TripMemberAdmissionFenceDocument? MemberAdmissionFence { get; set; }

    [BsonElement("memberAdmissionGeneration")]
    public long MemberAdmissionGeneration { get; set; }

    [BsonElement("admissionClosureState")]
    [BsonRepresentation(BsonType.String)]
    public TripAdmissionClosureState AdmissionClosureState { get; set; }

    [BsonElement("deletionState")]
    [BsonRepresentation(BsonType.String)]
    public TripDeletionState DeletionState { get; set; }

    [BsonElement("childMutationEpoch")]
    public long ChildMutationEpoch { get; set; } = 1;

    [BsonElement("childMutationLeaseSequence")]
    public long ChildMutationLeaseSequence { get; set; }

    [BsonElement("activeChildMutationLeases")]
    public List<TripChildMutationLeaseDocument> ActiveChildMutationLeases { get; set; } = new();

    [BsonElement("parkCandidateOrderIds")]
    public List<string> ParkCandidateOrderIds { get; set; } = new();

    [BsonElement("parkCandidateOrderVersion")]
    public long ParkCandidateOrderVersion { get; set; }

    [BsonElement("ownerSlot")]
    public int OwnerSlot { get; set; }

    [BsonElement("creationOperationKeyHash")]
    public string CreationOperationKeyHash { get; set; } = string.Empty;

    [BsonElement("creationPayloadHash")]
    public string CreationPayloadHash { get; set; } = string.Empty;

    [BsonElement("creationFingerprintKeyVersion")]
    public string CreationFingerprintKeyVersion { get; set; } = string.Empty;

    [BsonElement("creationSnapshot")]
    [BsonIgnoreIfNull]
    public TripPlanCreationSnapshotDocument? CreationSnapshot { get; set; }

    [BsonElement("creationOperationExpiresAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? CreationOperationExpiresAtUtc { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }
}
