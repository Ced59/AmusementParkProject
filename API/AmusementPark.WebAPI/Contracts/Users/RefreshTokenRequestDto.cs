namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP de demande de refresh token.
/// </summary>
public sealed class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}
