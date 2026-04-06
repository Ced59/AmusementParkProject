namespace AmusementPark.Application.Features.Users.Contracts;

/// <summary>
/// Demande applicative de changement de mot de passe.
/// </summary>
public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;

    public string VerifyNewPassword { get; init; } = string.Empty;
}
