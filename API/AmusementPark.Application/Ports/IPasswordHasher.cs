namespace AmusementPark.Application.Ports;

/// <summary>
/// Port applicatif de hashage et de vérification de mots de passe.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash un mot de passe en clair.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Vérifie un mot de passe en clair face à un hash stocké.
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);
}
