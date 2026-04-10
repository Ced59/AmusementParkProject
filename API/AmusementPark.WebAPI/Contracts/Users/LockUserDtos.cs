namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP de blocage d'utilisateur.
/// </summary>
public sealed class UserToLockDto
{
    public string IdUser { get; set; } = string.Empty;
}

/// <summary>
/// Contrat HTTP retourné après blocage d'utilisateur.
/// </summary>
public sealed class UserLockedDto
{
    public string UserId { get; set; } = string.Empty;

    public string? FirstName { get; set; } = string.Empty;

    public string? LastName { get; set; } = string.Empty;
}

/// <summary>
/// Contrat HTTP de déblocage d'utilisateur.
/// </summary>
public sealed class UserToUnlockDto
{
    public string IdUser { get; set; } = string.Empty;
}

/// <summary>
/// Contrat HTTP retourné après déblocage d'utilisateur.
/// </summary>
public sealed class UserUnlockedDto
{
    public string UserId { get; set; } = string.Empty;

    public string? FirstName { get; set; } = string.Empty;

    public string? LastName { get; set; } = string.Empty;
}
