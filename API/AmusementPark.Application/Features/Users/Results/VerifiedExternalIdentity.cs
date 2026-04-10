using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Results;

/// <summary>
/// Identité externe vérifiée par un fournisseur tiers.
/// </summary>
public sealed class VerifiedExternalIdentity
{
    public ExternalLoginProvider Provider { get; init; }

    public string ProviderUserId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public bool IsEmailVerified { get; init; }

    public bool IsEmailAuthoritative { get; init; }

    public string? DisplayName { get; init; }

    public string? GivenName { get; init; }

    public string? FamilyName { get; init; }

    public string? PictureUrl { get; init; }

    public string? HostedDomain { get; init; }
}
