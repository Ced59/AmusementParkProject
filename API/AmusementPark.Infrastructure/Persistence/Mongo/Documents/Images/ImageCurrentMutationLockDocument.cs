using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;

[BsonIgnoreExtraElements]
public sealed class ImageCurrentMutationLockDocument
{
    [BsonId]
    public string ScopeKey { get; set; } = string.Empty;

    [BsonElement("token")]
    public string? Token { get; set; }

    [BsonElement("expiresAtUtc")]
    public DateTime? ExpiresAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}
