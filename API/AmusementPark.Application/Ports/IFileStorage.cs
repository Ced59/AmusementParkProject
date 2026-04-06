namespace AmusementPark.Application.Ports
{
    /// <summary>
    /// Port applicatif de stockage de fichiers.
    /// </summary>
    public interface IFileStorage
    {
        /// <summary>
        /// Sauvegarde un flux binaire.
        /// </summary>
        Task<string> SaveAsync(string path, Stream content, string contentType, CancellationToken cancellationToken);

        /// <summary>
        /// Supprime un objet stocké.
        /// </summary>
        Task DeleteAsync(string path, CancellationToken cancellationToken);
    }
}
