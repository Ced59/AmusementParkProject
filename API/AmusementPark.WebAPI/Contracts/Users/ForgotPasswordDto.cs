namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP d'oubli de mot de passe.
/// </summary>
public sealed class ForgotPasswordDto
{
    public string Email { get; set; } = string.Empty;
}
