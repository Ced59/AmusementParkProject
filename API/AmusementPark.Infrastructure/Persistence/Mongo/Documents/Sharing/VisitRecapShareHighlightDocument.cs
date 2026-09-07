using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class VisitRecapShareHighlightDocument
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("rideCount")]
    [BsonIgnoreIfNull]
    public int? RideCount { get; set; }

    [BsonElement("rating")]
    [BsonIgnoreIfNull]
    public double? Rating { get; set; }
}
