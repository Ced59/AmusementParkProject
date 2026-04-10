namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP d'oubli de mot de passe.
/// </summary>
public sealed class ForgotPasswordDto
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Contrat HTTP retourné après demande d'oubli de mot de passe.
/// </summary>
public sealed class EmailPasswordSendedDto
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Contrat HTTP de réinitialisation de mot de passe.
/// </summary>
public sealed class ResetPasswordDto
{
    public string Token { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;

    public string NewPasswordConfirm { get; set; } = string.Empty;
}

/// <summary>
/// Contrat HTTP retourné après réinitialisation de mot de passe.
/// </summary>
public sealed class PasswordResetedDto
{
    public string Message { get; set; } = string.Empty;
}
