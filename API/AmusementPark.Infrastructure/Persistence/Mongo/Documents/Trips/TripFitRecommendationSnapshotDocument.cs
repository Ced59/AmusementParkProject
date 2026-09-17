using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

[BsonIgnoreExtraElements]
public sealed class TripFitRecommendationSnapshotDocument
{
    [BsonElement("methodVersion")]
    public string MethodVersion { get; set; } = string.Empty;

    [BsonElement("explanation")]
    public string Explanation { get; set; } = string.Empty;

    [BsonElement("calculatedAtUtc")]
    public DateTime CalculatedAtUtc { get; set; }
}
