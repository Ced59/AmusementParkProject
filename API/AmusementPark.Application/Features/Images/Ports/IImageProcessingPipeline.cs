using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Core.Domain.Images;

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
    /// Extrait des métadonnées depuis un contenu image brut.
    /// </summary>
    Task<ImageExifMetadata?> ExtractMetadataAsync(ImageUploadRequest request, CancellationToken cancellationToken);
}
