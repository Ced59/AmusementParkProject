using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Interfaces.Images
{
    public interface IImageStorageService
    {
        Task<IEnumerable<string>> StoreAsync(Dictionary<string, byte[]> images, string category);

        /// <summary>
        /// Récupère le meilleur format d'image (webp, jpg, png) à partir
        /// du chemin logique SANS extension (ex: "park_logo/72ac58...").
        /// </summary>
        Task<(Stream Stream, string ContentType)?> GetBestImageAsync(
            string imagePathWithoutExtension,
            string? acceptHeader,
            CancellationToken cancellationToken = default);
    }
}