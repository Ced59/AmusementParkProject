namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP de création d'utilisateur.
/// </summary>
public sealed class UserCreateDto
{
    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? VerifyPassword { get; set; }

    public string? PreferredLanguage { get; set; }

    public string? PreferredMeasurementSystem { get; set; }
}
