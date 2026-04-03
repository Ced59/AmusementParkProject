using Entities.Model.Images;

namespace Repositories.Interfaces
{
    public interface IImagesQueryHandler
    {
        Task<Image?> GetImageByIdAsync(string id);
        Task<Image?> CreateImageAsync(Image image);
        // On pourra ajouter plus tard : GetByCategory, pagination, etc.
    }
}