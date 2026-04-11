namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP retourné après authentification.
/// </summary>
public sealed class UserLoggedDto
{
    public string Token { get; set; } = string.Empty;
}
