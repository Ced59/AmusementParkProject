namespace AmusementPark.Application.Features.Users.Contracts;

/// <summary>
/// Données d'inscription locale d'un utilisateur.
/// </summary>
public sealed class RegisterUserRequest
{
    public string? Email { get; init; }

    public string? Password { get; init; }

    public string? VerifyPassword { get; init; }

    public string? PreferredLanguage { get; init; }
}
