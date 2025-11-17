using Entities.Model.Images;
using MongoDB.Driver;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class ImagesMongoQueryHandler : IImagesQueryHandler
    {
        private readonly IMongoCollection<Image> imagesCollection;

        public ImagesMongoQueryHandler(IMongoDatabase database, IMongoDbSettings settings)
        {
            imagesCollection = database.GetCollection<Image>(settings.ImagesCollectionName);
        }

        public async Task<Image?> GetImageByIdAsync(string id)
        {
            return await imagesCollection
                .Find(img => img.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<Image?> CreateImageAsync(Image image)
        {
            try
            {
                await imagesCollection.InsertOneAsync(image);
                return image;
            }
            catch
            {
                // Tu pourras logger plus finement si besoin
                return null;
            }
        }
    }
}