namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP retourné après blocage d'utilisateur.
/// </summary>
public sealed class UserLockedDto
{
    public string UserId { get; set; } = string.Empty;

    public string? FirstName { get; set; } = string.Empty;

    public string? LastName { get; set; } = string.Empty;
}
