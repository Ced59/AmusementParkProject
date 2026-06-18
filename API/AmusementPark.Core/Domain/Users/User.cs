using AmusementPark.Core.Abstractions;

namespace AmusementPark.Core.Domain.Users;

/// <summary>
/// Agrégat métier représentant un utilisateur.
/// </summary>
public sealed class User : AuditableEntity
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? HashedPassword { get; set; }

    public bool IsActivated { get; set; }

    public bool IsBlocked { get; set; }

    public string? PreferredLanguage { get; set; }

    public string? PreferredMeasurementSystem { get; set; }

    public string? AvatarUrl { get; set; }

    public List<Role> Roles { get; set; } = new();

    public List<ExternalLogin> ExternalLogins { get; set; } = new();

    public DateTime LastLoginUtc { get; set; }

    public DateTime LastActivityUtc { get; set; }

    public string? EmailConfirmationTokenHash { get; set; }

    public DateTime? EmailConfirmationTokenExpiresAtUtc { get; set; }

    public DateTime? EmailConfirmationSentAtUtc { get; set; }

    public string? PasswordResetTokenHash { get; set; }

    public DateTime? PasswordResetTokenExpiresAtUtc { get; set; }

    public DateTime? PasswordResetSentAtUtc { get; set; }

    /// <summary>
    /// Indique si l'utilisateur possède déjà le rôle demandé.
    /// </summary>
    /// <param name="role">Rôle recherché.</param>
    /// <returns>Vrai si le rôle est présent.</returns>
    public bool HasRole(Role role)
    {
        return Roles.Contains(role);
    }
}
