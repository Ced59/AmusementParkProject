namespace AmusementPark.Application.Ports;

/// <summary>
/// Port applicatif d'import d'avatar utilisateur distant.
/// </summary>
public interface IUserAvatarImporter
{
    /// <summary>
    /// Télécharge un avatar distant et retourne son URL ou chemin applicatif.
    /// </summary>
    Task<string> DownloadAndSaveAsync(string imageUrl, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Télécharge un avatar en conservant séparément l'annulation de cohérence
    /// qui doit rester active après l'acquisition du verrou d'image.
    /// </summary>
    Task<string> DownloadAndSaveAsync(
        string imageUrl,
        string userId,
        CancellationToken cancellationToken,
        CancellationToken consistencyCancellationToken);
}
