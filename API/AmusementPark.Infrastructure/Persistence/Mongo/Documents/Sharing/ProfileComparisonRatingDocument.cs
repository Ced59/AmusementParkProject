using AmusementPark.Core.Domain.Sharing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

public sealed class ProfileComparisonRatingDocument
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

    [BsonElement("creatorRating")]
    public double CreatorRating { get; set; }

    [BsonElement("acceptorRating")]
    public double AcceptorRating { get; set; }

    [BsonElement("absoluteDifference")]
    public double AbsoluteDifference { get; set; }

    [BsonElement("affinity")]
    [BsonRepresentation(BsonType.String)]
    public ProfileComparisonRatingAffinity Affinity { get; set; }
}
