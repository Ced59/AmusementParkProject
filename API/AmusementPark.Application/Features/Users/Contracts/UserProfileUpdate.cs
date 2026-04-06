namespace AmusementPark.Application.Features.Users.Contracts;

/// <summary>
/// Données applicatives de mise à jour du profil utilisateur.
/// </summary>
public sealed class UserProfileUpdate
{
    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Email { get; init; }

    public string? NewEmail { get; init; }

    public string? PreferredLanguage { get; init; }

    public string? AvatarUrl { get; init; }
}
