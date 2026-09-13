using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;

/// <summary>
/// Lien persistant vers un compte externe.
/// </summary>
public sealed class ExternalLoginDocument
{
    [BsonElement("provider")]
    [BsonRepresentation(BsonType.String)]
    public ExternalLoginProvider Provider { get; set; }

    [BsonElement("providerUserId")]
    public string ProviderUserId { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("isEmailVerified")]
    public bool IsEmailVerified { get; set; }

    [BsonElement("displayName")]
    [BsonIgnoreIfNull]
    public string? DisplayName { get; set; }

    [BsonElement("givenName")]
    [BsonIgnoreIfNull]
    public string? GivenName { get; set; }

    [BsonElement("familyName")]
    [BsonIgnoreIfNull]
    public string? FamilyName { get; set; }

    [BsonElement("pictureUrl")]
    [BsonIgnoreIfNull]
    public string? PictureUrl { get; set; }

    [BsonElement("hostedDomain")]
    [BsonIgnoreIfNull]
    public string? HostedDomain { get; set; }

    [BsonElement("linkedAtUtc")]
    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;

    [BsonElement("lastLoginAtUtc")]
    public DateTime LastLoginAtUtc { get; set; } = DateTime.UtcNow;
}
