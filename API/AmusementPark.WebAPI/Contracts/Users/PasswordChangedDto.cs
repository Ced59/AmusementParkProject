namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP retourné après changement de mot de passe.
/// </summary>
public sealed class PasswordChangedDto
{
    public string Message { get; set; } = string.Empty;
}
