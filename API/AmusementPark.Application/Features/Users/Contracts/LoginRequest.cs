namespace AmusementPark.Application.Features.Users.Contracts;

/// <summary>
/// Demande de connexion locale.
/// </summary>
public sealed class LoginRequest
{
    public string? Email { get; init; }

    public string? Password { get; init; }
}
