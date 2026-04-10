using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Contracts;

/// <summary>
/// Données applicatives d'authentification / provisionnement externe.
/// </summary>
public sealed class ProvisionExternalUserRequest
{
    public ExternalLoginProvider Provider { get; init; }

    public string Token { get; init; } = string.Empty;

    public string? Nonce { get; init; }

    public string? PreferredLanguage { get; init; }
}
