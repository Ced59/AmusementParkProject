using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class VisitRecapShareItemDocument
{
    [BsonElement("parkItemId")]
    public string ParkItemId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("category")]
    [BsonIgnoreIfNull]
    public string? Category { get; set; }

    [BsonElement("rideCount")]
    [BsonIgnoreIfNull]
    public int? RideCount { get; set; }

    [BsonElement("averageRating")]
    [BsonIgnoreIfNull]
    public double? AverageRating { get; set; }

    [BsonElement("isMissed")]
    public bool IsMissed { get; set; }
}
