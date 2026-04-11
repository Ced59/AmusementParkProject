using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;

/// <summary>
/// Document Mongo des refresh tokens opaques.
/// </summary>
[BsonIgnoreExtraElements]
public sealed class RefreshTokenDocument : MongoDocumentBase
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("tokenHash")]
    public string TokenHash { get; set; } = string.Empty;

    [BsonElement("expiresAtUtc")]
    public DateTime ExpiresAtUtc { get; set; }

    [BsonElement("lastUsedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? LastUsedAtUtc { get; set; }

    [BsonElement("revokedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? RevokedAtUtc { get; set; }

    [BsonElement("replacedByTokenHash")]
    [BsonIgnoreIfNull]
    public string? ReplacedByTokenHash { get; set; }

    [BsonElement("revocationReason")]
    [BsonIgnoreIfNull]
    public string? RevocationReason { get; set; }
}
