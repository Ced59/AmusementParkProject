using AmusementPark.Core.Domain.ParkFit;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;

[BsonIgnoreExtraElements]
public sealed class ParkFitOperationalDecisionDocument
{
    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public ParkFitOperationalDecisionType Type { get; set; }

    [BsonElement("actorUserId")]
    public string ActorUserId { get; set; } = string.Empty;

    [BsonElement("reason")]
    public string Reason { get; set; } = string.Empty;

    [BsonElement("decidedAtUtc")]
    public DateTime DecidedAtUtc { get; set; }

    [BsonElement("revision")]
    public long Revision { get; set; }
}
