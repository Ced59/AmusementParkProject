using Entities.Model.Images;
using MongoDB.Driver;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public sealed class ImageTagsMongoQueryHandler : IImageTagsQueryHandler
    {
        private readonly IMongoCollection<ImageTag> collection;

        public ImageTagsMongoQueryHandler(IMongoDatabase database, IMongoDbSettings settings)
        {
            collection = database.GetCollection<ImageTag>(settings.ImageTagsCollectionName);
        }

        public async Task<IReadOnlyList<ImageTag>> GetAllAsync()
        {
            return await collection.Find(Builders<ImageTag>.Filter.Empty).SortBy(x => x.Slug).ToListAsync();
        }

        public async Task<ImageTag?> GetByIdAsync(string id)
        {
            return await collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<ImageTag?> GetBySlugAsync(string slug)
        {
            return await collection.Find(x => x.Slug == slug).FirstOrDefaultAsync();
        }

        public async Task<ImageTag?> CreateAsync(ImageTag tag)
        {
            await collection.InsertOneAsync(tag);
            return tag;
        }

        public async Task<ImageTag?> UpdateAsync(ImageTag tag)
        {
            ReplaceOneResult result = await collection.ReplaceOneAsync(x => x.Id == tag.Id, tag);
            return result.MatchedCount > 0 ? tag : null;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            DeleteResult result = await collection.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
