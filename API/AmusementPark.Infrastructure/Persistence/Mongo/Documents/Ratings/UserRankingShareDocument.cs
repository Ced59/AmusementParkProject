using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;

[BsonIgnoreExtraElements]
public sealed class UserRankingShareDocument : MongoDocumentBase
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("isPublic")]
    public bool IsPublic { get; set; }

    [BsonElement("shareId")]
    [BsonIgnoreIfNull]
    public string? ShareId { get; set; }

    [BsonElement("publishedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? PublishedAtUtc { get; set; }
}
