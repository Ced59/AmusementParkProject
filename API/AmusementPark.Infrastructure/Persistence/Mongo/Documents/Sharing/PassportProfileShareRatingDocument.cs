using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class PassportProfileShareRatingDocument
{
    [BsonElement("targetType")]
    public string TargetType { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("parkName")]
    [BsonIgnoreIfNull]
    public string? ParkName { get; set; }

    [BsonElement("category")]
    [BsonIgnoreIfNull]
    public string? Category { get; set; }

    [BsonElement("rating")]
    public double Rating { get; set; }
}
