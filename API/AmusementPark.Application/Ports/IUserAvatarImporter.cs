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
}
