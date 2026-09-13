namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP retourné après réinitialisation de mot de passe.
/// </summary>
public sealed class PasswordResetedDto
{
    public string Message { get; set; } = string.Empty;
}
