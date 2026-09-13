namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP de déblocage d'utilisateur.
/// </summary>
public sealed class UserToUnlockDto
{
    public string IdUser { get; set; } = string.Empty;
}
