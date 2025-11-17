namespace Services.Interfaces.Storage
{
    public interface IImageStorageService
    {
        /// <summary>
        /// Renvoie le meilleur format d'image (webp si possible) à partir de l'id logique d'image.
        /// </summary>
        /// <param name="imageId">Id logique de l'image (ex: guid, sans extension).</param>
        /// <param name="acceptHeader">Header HTTP Accept du client.</param>
        /// <returns>
        /// (Stream, ContentType) ou null si aucune image trouvée.
        /// </returns>
        Task<(Stream Stream, string ContentType)?> GetBestImageAsync(
            string imageId,
            string? acceptHeader,
            CancellationToken cancellationToken = default);
    }
}