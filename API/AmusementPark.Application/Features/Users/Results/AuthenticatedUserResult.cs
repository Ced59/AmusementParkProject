using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Results;

/// <summary>
/// Résultat applicatif d'authentification.
/// </summary>
public sealed class AuthenticatedUserResult
{
    public User User { get; init; } = new();

    public string AccessToken { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;

    public DateTime RefreshTokenExpiresAtUtc { get; init; }
}
