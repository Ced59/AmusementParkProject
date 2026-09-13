using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;

[BsonIgnoreExtraElements]
public sealed class UserRatingDocument : MongoDocumentBase
{
    [BsonElement("isMutationPlaceholder")]
    [BsonIgnoreIfDefault]
    public bool IsMutationPlaceholder { get; set; }

    [BsonElement("activeRankingMutationToken")]
    [BsonIgnoreIfNull]
    public string? ActiveRankingMutationToken { get; set; }

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("targetType")]
    [BsonRepresentation(BsonType.String)]
    public RatingTargetType TargetType { get; set; }

    [BsonElement("targetId")]
    public string TargetId { get; set; } = string.Empty;

    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("parkItemCategory")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.String)]
    public ParkItemCategory? ParkItemCategory { get; set; }

    [BsonElement("parkItemType")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.String)]
    public ParkItemType? ParkItemType { get; set; }

    [BsonElement("value")]
    [BsonRepresentation(BsonType.Double)]
    public double Value { get; set; }
}
