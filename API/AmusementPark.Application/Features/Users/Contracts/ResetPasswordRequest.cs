namespace AmusementPark.Application.Features.Users.Contracts;

/// <summary>
/// Demande applicative de réinitialisation de mot de passe.
/// </summary>
public sealed class ResetPasswordRequest
{
    public string Token { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;

    public string VerifyNewPassword { get; init; } = string.Empty;
}
