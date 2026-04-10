namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP retourné après refresh token.
/// </summary>
public sealed class RefreshTokenResponseDto
{
    public string RefreshToken { get; set; } = string.Empty;
}
