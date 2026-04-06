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
            return await imagesCollection.Find(img => img.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Image?> CreateImageAsync(Image image)
        {
            await imagesCollection.InsertOneAsync(image);
            return image;
        }

        public async Task<Image?> UpdateImageAsync(Image image)
        {
            ReplaceOneResult result = await imagesCollection.ReplaceOneAsync(img => img.Id == image.Id, image);
            return result.MatchedCount > 0 ? image : null;
        }

        public async Task<bool> DeleteImageAsync(string id)
        {
            DeleteResult result = await imagesCollection.DeleteOneAsync(img => img.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<IReadOnlyList<Image>> GetImagesByOwnerAsync(ImageOwnerType ownerType, string ownerId, ImageCategory? category = null)
        {
            FilterDefinition<Image> filter = Builders<Image>.Filter.Eq(img => img.OwnerType, ownerType) & Builders<Image>.Filter.Eq(img => img.OwnerId, ownerId);
            if (category.HasValue)
            {
                filter &= Builders<Image>.Filter.Eq(img => img.Category, category.Value);
            }
            return await imagesCollection.Find(filter).SortByDescending(img => img.CreatedAt).ToListAsync();
        }

        public async Task<Image?> GetCurrentImageByOwnerAsync(ImageOwnerType ownerType, string ownerId, ImageCategory category)
        {
            return await imagesCollection.Find(img => img.OwnerType == ownerType && img.OwnerId == ownerId && img.Category == category && img.IsCurrent).FirstOrDefaultAsync();
        }

        public async Task<bool> UnsetCurrentImagesAsync(ImageOwnerType ownerType, string ownerId, ImageCategory category, string? excludeImageId = null)
        {
            FilterDefinition<Image> filter = Builders<Image>.Filter.Eq(img => img.OwnerType, ownerType) & Builders<Image>.Filter.Eq(img => img.OwnerId, ownerId) & Builders<Image>.Filter.Eq(img => img.Category, category) & Builders<Image>.Filter.Eq(img => img.IsCurrent, true);
            if (!string.IsNullOrWhiteSpace(excludeImageId))
            {
                filter &= Builders<Image>.Filter.Ne(img => img.Id, excludeImageId);
            }
            UpdateDefinition<Image> update = Builders<Image>.Update.Set(img => img.IsCurrent, false).Set(img => img.UpdatedAt, DateTime.UtcNow);
            UpdateResult result = await imagesCollection.UpdateManyAsync(filter, update);
            return result.IsAcknowledged;
        }

        public async Task<IReadOnlyList<Image>> GetAllImagesAsync()
        {
            return await imagesCollection.Find(Builders<Image>.Filter.Empty).SortByDescending(x => x.CreatedAt).ToListAsync();
        }
    }
}
