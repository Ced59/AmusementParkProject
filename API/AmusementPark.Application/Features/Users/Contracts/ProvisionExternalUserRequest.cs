using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Contracts;

/// <summary>
/// Données applicatives de provisionnement d'un utilisateur externe.
/// </summary>
public sealed class ProvisionExternalUserRequest
{
    public ExternalLoginProvider Provider { get; init; }

    public string ProviderToken { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? PreferredLanguage { get; init; }
}
