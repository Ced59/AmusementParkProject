using AmusementPark.Application.Features.CaptainCoaster.Results;
using AmusementPark.Core.Domain.Countries;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Countries;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Search;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

/// <summary>
/// Mappers centralisés domaine/resultats applicatifs &lt;-&gt; documents Mongo.
/// </summary>
internal static partial class EntityMongoMappers
{
    public static User ToDomain(this UserDocument document)
    {
        User entity = new User
        {
            Id = document.Id,
            FirstName = document.FirstName,
            LastName = document.LastName,
            Email = document.Email,
            HashedPassword = document.HashedPassword,
            IsActivated = document.IsActivated,
            IsBlocked = document.IsBlocked,
            PreferredLanguage = document.PreferredLanguage,
            PreferredMeasurementSystem = document.PreferredMeasurementSystem,
            AvatarUrl = document.AvatarUrl,
            Roles = document.Roles.ToList(),
            ExternalLogins = document.ExternalLogins.Select(ToDomain).ToList(),
            LastLoginUtc = document.LastLoginUtc,
            LastActivityUtc = document.LastActivityUtc,
            EmailConfirmationTokenHash = document.EmailConfirmationTokenHash,
            EmailConfirmationTokenExpiresAtUtc = document.EmailConfirmationTokenExpiresAtUtc,
            EmailConfirmationSentAtUtc = document.EmailConfirmationSentAtUtc,
            PasswordResetTokenHash = document.PasswordResetTokenHash,
            PasswordResetTokenExpiresAtUtc = document.PasswordResetTokenExpiresAtUtc,
            PasswordResetSentAtUtc = document.PasswordResetSentAtUtc,
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static UserDocument ToDocument(this User entity)
    {
        return new UserDocument
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Email = entity.Email,
            HashedPassword = entity.HashedPassword,
            IsActivated = entity.IsActivated,
            IsBlocked = entity.IsBlocked,
            PreferredLanguage = entity.PreferredLanguage,
            PreferredMeasurementSystem = entity.PreferredMeasurementSystem,
            AvatarUrl = entity.AvatarUrl,
            Roles = entity.Roles.ToList(),
            ExternalLogins = entity.ExternalLogins.Select(ToDocument).ToList(),
            LastLoginUtc = entity.LastLoginUtc,
            LastActivityUtc = entity.LastActivityUtc,
            EmailConfirmationTokenHash = entity.EmailConfirmationTokenHash,
            EmailConfirmationTokenExpiresAtUtc = entity.EmailConfirmationTokenExpiresAtUtc,
            EmailConfirmationSentAtUtc = entity.EmailConfirmationSentAtUtc,
            PasswordResetTokenHash = entity.PasswordResetTokenHash,
            PasswordResetTokenExpiresAtUtc = entity.PasswordResetTokenExpiresAtUtc,
            PasswordResetSentAtUtc = entity.PasswordResetSentAtUtc,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static RefreshToken ToDomain(this RefreshTokenDocument document)
    {
        RefreshToken entity = new RefreshToken
        {
            Id = document.Id,
            UserId = document.UserId,
            TokenHash = document.TokenHash,
            ExpiresAtUtc = document.ExpiresAtUtc,
            LastUsedAtUtc = document.LastUsedAtUtc,
            RevokedAtUtc = document.RevokedAtUtc,
            ReplacedByTokenHash = document.ReplacedByTokenHash,
            RevocationReason = document.RevocationReason,
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static RefreshTokenDocument ToDocument(this RefreshToken entity)
    {
        return new RefreshTokenDocument
        {
            Id = entity.Id,
            UserId = entity.UserId,
            TokenHash = entity.TokenHash,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            LastUsedAtUtc = entity.LastUsedAtUtc,
            RevokedAtUtc = entity.RevokedAtUtc,
            ReplacedByTokenHash = entity.ReplacedByTokenHash,
            RevocationReason = entity.RevocationReason,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static ExternalLogin ToDomain(this ExternalLoginDocument document)
    {
        return new ExternalLogin
        {
            Provider = document.Provider,
            ProviderUserId = document.ProviderUserId,
            Email = document.Email,
            IsEmailVerified = document.IsEmailVerified,
            DisplayName = document.DisplayName,
            GivenName = document.GivenName,
            FamilyName = document.FamilyName,
            PictureUrl = document.PictureUrl,
            HostedDomain = document.HostedDomain,
            LinkedAtUtc = document.LinkedAtUtc,
            LastLoginAtUtc = document.LastLoginAtUtc,
        };
    }

    public static ExternalLoginDocument ToDocument(this ExternalLogin entity)
    {
        return new ExternalLoginDocument
        {
            Provider = entity.Provider,
            ProviderUserId = entity.ProviderUserId,
            Email = entity.Email,
            IsEmailVerified = entity.IsEmailVerified,
            DisplayName = entity.DisplayName,
            GivenName = entity.GivenName,
            FamilyName = entity.FamilyName,
            PictureUrl = entity.PictureUrl,
            HostedDomain = entity.HostedDomain,
            LinkedAtUtc = entity.LinkedAtUtc,
            LastLoginAtUtc = entity.LastLoginAtUtc,
        };
    }
}
