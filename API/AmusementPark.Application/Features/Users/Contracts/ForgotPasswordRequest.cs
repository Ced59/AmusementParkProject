namespace AmusementPark.Application.Features.Users.Contracts;

/// <summary>
/// Demande applicative d'oubli de mot de passe.
/// </summary>
public sealed class ForgotPasswordRequest
{
    public string Email { get; init; } = string.Empty;
}
