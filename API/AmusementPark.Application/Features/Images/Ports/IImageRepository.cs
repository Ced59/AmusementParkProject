using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Ports;

/// <summary>
/// Port applicatif de persistance des images.
/// </summary>
public interface IImageRepository
{
    Task<Image?> GetByIdAsync(string imageId, CancellationToken cancellationToken);
    Task<Image> CreateAsync(ImageUploadRequest request, CancellationToken cancellationToken);
    Task<Image?> LinkAsync(string imageId, ImageOwnerType ownerType, string ownerId, CancellationToken cancellationToken);
    Task<Image?> SetCurrentAsync(string imageId, ImageOwnerType ownerType, string ownerId, CancellationToken cancellationToken);
    Task<Image?> UpdateMetadataAsync(string imageId, ImageMetadataUpdate metadata, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string imageId, CancellationToken cancellationToken);
}
