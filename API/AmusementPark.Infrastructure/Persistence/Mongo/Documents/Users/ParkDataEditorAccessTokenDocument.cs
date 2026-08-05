using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;

[BsonIgnoreExtraElements]
public sealed class ParkDataEditorAccessTokenDocument : MongoDocumentBase
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("label")]
    public string Label { get; set; } = string.Empty;

    [BsonElement("tokenHash")]
    public string TokenHash { get; set; } = string.Empty;

    [BsonElement("displayPrefix")]
    public string DisplayPrefix { get; set; } = string.Empty;

    [BsonElement("expiresAtUtc")]
    public DateTime ExpiresAtUtc { get; set; }

    [BsonElement("lastUsedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? LastUsedAtUtc { get; set; }

    [BsonElement("revokedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? RevokedAtUtc { get; set; }

    [BsonElement("revokedByUserId")]
    [BsonIgnoreIfNull]
    public string? RevokedByUserId { get; set; }

    [BsonElement("revocationReason")]
    [BsonIgnoreIfNull]
    public string? RevocationReason { get; set; }
}
