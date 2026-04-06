namespace AmusementPark.Application.Ports;

/// <summary>
/// Port applicatif de hachage de mot de passe.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hache un mot de passe en clair.
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Vérifie un mot de passe en clair contre un hash.
    /// </summary>
    bool Verify(string password, string hash);
}
