namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP de blocage d'utilisateur.
/// </summary>
public sealed class UserToLockDto
{
    public string IdUser { get; set; } = string.Empty;
}
