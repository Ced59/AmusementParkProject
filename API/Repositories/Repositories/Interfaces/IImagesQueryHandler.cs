using Entities.Model.Images;

namespace Repositories.Interfaces
{
    public interface IImagesQueryHandler
    {
        Task<Image?> GetImageByIdAsync(string id);
        Task<Image?> CreateImageAsync(Image image);
        Task<Image?> UpdateImageAsync(Image image);
        Task<bool> DeleteImageAsync(string id);
        Task<IReadOnlyList<Image>> GetImagesByOwnerAsync(ImageOwnerType ownerType, string ownerId, ImageCategory? category = null);
        Task<Image?> GetCurrentImageByOwnerAsync(ImageOwnerType ownerType, string ownerId, ImageCategory category);
        Task<bool> UnsetCurrentImagesAsync(ImageOwnerType ownerType, string ownerId, ImageCategory category, string? excludeImageId = null);
        Task<IReadOnlyList<Image>> GetAllImagesAsync();
    }
}
