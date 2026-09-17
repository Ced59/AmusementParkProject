using AmusementPark.Core.Domain.Trips;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

[BsonIgnoreExtraElements]
public sealed class TripMemberDocument
{
    [BsonElement("memberId")]
    public string MemberId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("delegatedRole")]
    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public TripDelegatedRole? DelegatedRole { get; set; }

    [BsonElement("state")]
    [BsonRepresentation(BsonType.String)]
    public TripMembershipState State { get; set; }

    [BsonElement("joinedAtUtc")]
    public DateTime JoinedAtUtc { get; set; }
}
