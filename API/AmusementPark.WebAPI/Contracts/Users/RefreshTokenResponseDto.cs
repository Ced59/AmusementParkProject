namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP retourné après refresh token.
/// </summary>
public sealed class RefreshTokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime RefreshTokenExpiresAtUtc { get; set; }
}
