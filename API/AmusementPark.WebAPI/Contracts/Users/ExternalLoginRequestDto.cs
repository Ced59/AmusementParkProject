namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP d'authentification externe.
/// </summary>
public sealed class ExternalLoginRequestDto
{
    public string Token { get; set; } = string.Empty;

    public string? Nonce { get; set; }

    public string? PreferredMeasurementSystem { get; set; }
}
