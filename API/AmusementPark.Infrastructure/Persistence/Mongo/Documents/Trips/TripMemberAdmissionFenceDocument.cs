using AmusementPark.Core.Domain.Trips;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

[BsonIgnoreExtraElements]
public sealed class TripMemberAdmissionFenceDocument
{
    [BsonElement("invitationId")]
    public string InvitationId { get; set; } = string.Empty;

    [BsonElement("operationId")]
    public string OperationId { get; set; } = string.Empty;

    [BsonElement("candidateUserId")]
    public string CandidateUserId { get; set; } = string.Empty;

    [BsonElement("generation")]
    public long Generation { get; set; }

    [BsonElement("leaseExpiresAtUtc")]
    public DateTime LeaseExpiresAtUtc { get; set; }

    [BsonElement("state")]
    [BsonRepresentation(BsonType.String)]
    public TripMemberAdmissionFenceState State { get; set; }
}
