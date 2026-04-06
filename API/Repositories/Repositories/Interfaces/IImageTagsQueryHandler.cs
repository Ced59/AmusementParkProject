using Entities.Model.Images;

namespace Repositories.Interfaces
{
    public interface IImageTagsQueryHandler
    {
        Task<IReadOnlyList<ImageTag>> GetAllAsync();
        Task<ImageTag?> GetByIdAsync(string id);
        Task<ImageTag?> GetBySlugAsync(string slug);
        Task<ImageTag?> CreateAsync(ImageTag tag);
        Task<ImageTag?> UpdateAsync(ImageTag tag);
        Task<bool> DeleteAsync(string id);
    }
}
