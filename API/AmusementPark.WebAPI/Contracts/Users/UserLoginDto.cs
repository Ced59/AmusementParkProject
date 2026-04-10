namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP de connexion locale.
/// </summary>
public sealed class UserLoginDto
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
