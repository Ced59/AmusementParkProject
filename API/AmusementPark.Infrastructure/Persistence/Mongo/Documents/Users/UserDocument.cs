using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;

/// <summary>
/// Document Mongo d'un utilisateur.
/// </summary>
[BsonIgnoreExtraElements]
public sealed class UserDocument : MongoDocumentBase
{
    [BsonElement("firstName")]
    [BsonIgnoreIfNull]
    public string? FirstName { get; set; }

    [BsonElement("lastName")]
    [BsonIgnoreIfNull]
    public string? LastName { get; set; }

    [BsonElement("email")]
    [BsonIgnoreIfNull]
    public string? Email { get; set; }

    [BsonElement("hashedPassword")]
    [BsonIgnoreIfNull]
    public string? HashedPassword { get; set; }

    [BsonElement("isActivated")]
    public bool IsActivated { get; set; }

    [BsonElement("isBlocked")]
    public bool IsBlocked { get; set; }

    [BsonElement("preferredLanguage")]
    [BsonIgnoreIfNull]
    public string? PreferredLanguage { get; set; }

    [BsonElement("avatarUrl")]
    [BsonIgnoreIfNull]
    public string? AvatarUrl { get; set; }

    [BsonElement("roles")]
    [BsonRepresentation(BsonType.String)]
    public List<Role> Roles { get; set; } = new();

    [BsonElement("externalLogins")]
    public List<ExternalLoginDocument> ExternalLogins { get; set; } = new();

    [BsonElement("lastLogin")]
    public DateTime LastLoginUtc { get; set; }

    [BsonElement("lastActivity")]
    public DateTime LastActivityUtc { get; set; }

    [BsonElement("emailConfirmationTokenHash")]
    [BsonIgnoreIfNull]
    public string? EmailConfirmationTokenHash { get; set; }

    [BsonElement("emailConfirmationTokenExpiresAt")]
    [BsonIgnoreIfNull]
    public DateTime? EmailConfirmationTokenExpiresAtUtc { get; set; }

    [BsonElement("emailConfirmationSentAt")]
    [BsonIgnoreIfNull]
    public DateTime? EmailConfirmationSentAtUtc { get; set; }

    [BsonElement("passwordResetTokenHash")]
    [BsonIgnoreIfNull]
    public string? PasswordResetTokenHash { get; set; }

    [BsonElement("passwordResetTokenExpiresAt")]
    [BsonIgnoreIfNull]
    public DateTime? PasswordResetTokenExpiresAtUtc { get; set; }

    [BsonElement("passwordResetSentAt")]
    [BsonIgnoreIfNull]
    public DateTime? PasswordResetSentAtUtc { get; set; }
}

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
