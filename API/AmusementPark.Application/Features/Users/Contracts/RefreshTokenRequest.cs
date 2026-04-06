namespace AmusementPark.Application.Features.Users.Contracts;

/// <summary>
/// Demande applicative de rafraîchissement de token.
/// </summary>
public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
