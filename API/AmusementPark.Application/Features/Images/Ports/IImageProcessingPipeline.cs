using AmusementPark.Application.Features.Images.Contracts;

namespace AmusementPark.Application.Features.Images.Ports;

/// <summary>
/// Pipeline technique d'enrichissement des images.
/// </summary>
public interface IImageProcessingPipeline
{
    /// <summary>
    /// Traite le contenu image brut avant persistance définitive.
    /// </summary>
    Task<ImageUploadRequest> ProcessAsync(ImageUploadRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Extrait les métadonnées depuis un contenu image brut.
    /// </summary>
    Task<ImageProcessingMetadata?> ExtractMetadataAsync(ImageUploadRequest request, CancellationToken cancellationToken);
}
