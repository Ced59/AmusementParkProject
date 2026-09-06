using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

/// <summary>
/// Projection en lecture seule de la collection gelée par la migration SHARE-04A.
/// </summary>
[BsonIgnoreExtraElements]
internal sealed class LegacyUserRankingShareDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

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

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
