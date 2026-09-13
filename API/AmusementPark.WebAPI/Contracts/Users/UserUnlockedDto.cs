namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP retourné après déblocage d'utilisateur.
/// </summary>
public sealed class UserUnlockedDto
{
    public string UserId { get; set; } = string.Empty;

    public string? FirstName { get; set; } = string.Empty;

    public string? LastName { get; set; } = string.Empty;
}
