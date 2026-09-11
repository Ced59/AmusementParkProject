using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class YearRecapShareHighlightDocument
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("rideCount")]
    public long RideCount { get; set; }

    [BsonElement("ratingCount")]
    public long RatingCount { get; set; }

    [BsonElement("averageRating")]
    [BsonIgnoreIfNull]
    public double? AverageRating { get; set; }

    [BsonElement("isNowClosed")]
    public bool IsNowClosed { get; set; }
}
