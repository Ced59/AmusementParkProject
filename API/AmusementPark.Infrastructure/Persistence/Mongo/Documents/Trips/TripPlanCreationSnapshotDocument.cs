using AmusementPark.Core.Domain.Trips;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

[BsonIgnoreExtraElements]
public sealed class TripPlanCreationSnapshotDocument
{
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

    [BsonElement("admissionClosureState")]
    [BsonRepresentation(BsonType.String)]
    public TripAdmissionClosureState AdmissionClosureState { get; set; }

    [BsonElement("deletionState")]
    [BsonRepresentation(BsonType.String)]
    public TripDeletionState DeletionState { get; set; }

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }
}
