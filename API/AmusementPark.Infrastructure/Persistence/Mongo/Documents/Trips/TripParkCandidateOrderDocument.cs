using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

[BsonIgnoreExtraElements]
public sealed class TripParkCandidateOrderDocument : MongoDocumentBase
{
    [BsonElement("candidateIds")]
    public List<string> CandidateIds { get; set; } = new();

    [BsonElement("version")]
    public long Version { get; set; }
}
