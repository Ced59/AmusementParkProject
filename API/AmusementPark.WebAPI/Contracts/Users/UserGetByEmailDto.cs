namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP de recherche d'utilisateur par email.
/// </summary>
public sealed class UserGetByEmailDto
{
    public string Email { get; set; } = string.Empty;
}
