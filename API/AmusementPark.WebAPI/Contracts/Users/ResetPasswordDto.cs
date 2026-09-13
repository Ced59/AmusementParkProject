namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP de réinitialisation de mot de passe.
/// </summary>
public sealed class ResetPasswordDto
{
    public string Token { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;

    public string NewPasswordConfirm { get; set; } = string.Empty;
}
