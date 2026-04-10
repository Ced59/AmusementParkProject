namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP de changement de mot de passe.
/// </summary>
public sealed class ChangePasswordDto
{
    public string ActualPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;

    public string NewPasswordConfirm { get; set; } = string.Empty;
}
