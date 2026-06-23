using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Ports;

/// <summary>
/// Port applicatif de persistance des images.
/// </summary>
public interface IImageRepository
{
    Task<IReadOnlyCollection<Image>> GetAllAsync(CancellationToken cancellationToken);
    Task<PagedResult<Image>> GetPageAsync(int page, int pageSize, ImageSearchCriteria criteria, CancellationToken cancellationToken);
    Task<Image?> GetByIdAsync(string imageId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Image>> GetByOwnerAsync(ImageOwnerType ownerType, string ownerId, ImageCategory? category, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Image>> GetByOwnersAsync(ImageOwnerType ownerType, IReadOnlyCollection<string> ownerIds, ImageCategory? category, CancellationToken cancellationToken);
    Task<Image?> GetByOwnerAndSourceUrlAsync(ImageOwnerType ownerType, string ownerId, string sourceUrl, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, string>> GetMainImageIdsByOwnersAsync(ImageOwnerType ownerType, IReadOnlyCollection<string> ownerIds, ImageCategory category, bool publishedOnly, CancellationToken cancellationToken);
    Task<Image?> GetCurrentByOwnerAsync(ImageOwnerType ownerType, string ownerId, ImageCategory category, CancellationToken cancellationToken);
    Task<Image> CreateAsync(ImageUploadRequest request, CancellationToken cancellationToken);
    Task<Image?> LinkAsync(string imageId, ImageOwnerType ownerType, string ownerId, CancellationToken cancellationToken);
    Task<Image?> SetCurrentAsync(string imageId, ImageOwnerType ownerType, string ownerId, CancellationToken cancellationToken);
    Task<Image?> UpdateMetadataAsync(string imageId, ImageMetadataUpdate metadata, CancellationToken cancellationToken);
    Task<Image?> MarkWatermarkedAsync(string imageId, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string imageId, CancellationToken cancellationToken);
    Task<int> UpdateBulkMetadataAsync(IReadOnlyCollection<string> imageIds, ImageBulkMetadataUpdate metadata, CancellationToken cancellationToken);
}
