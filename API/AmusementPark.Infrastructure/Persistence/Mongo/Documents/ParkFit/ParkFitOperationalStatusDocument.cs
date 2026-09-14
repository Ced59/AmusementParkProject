using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;

[BsonIgnoreExtraElements]
public sealed class ParkFitOperationalStatusDocument : MongoDocumentBase
{
    [BsonElement("state")]
    [BsonRepresentation(BsonType.String)]
    public ParkFitRecommendationState State { get; set; }

    [BsonElement("revision")]
    public long Revision { get; set; }

    [BsonElement("decisions")]
    public List<ParkFitOperationalDecisionDocument> Decisions { get; set; } = new();
}
