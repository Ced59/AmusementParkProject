namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP de demande de refresh token.
/// Le refresh token est désormais attendu en priorité via cookie HttpOnly.
/// </summary>
public sealed class RefreshTokenRequestDto
{
    public string? RefreshToken { get; set; }
}
