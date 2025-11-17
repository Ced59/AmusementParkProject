using Entities.Model.Parks;
using MongoDB.Driver;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class ParkLogosMongoQueryHandler : IParkLogosQueryHandler
    {
        private readonly IMongoCollection<ParkLogo> parkLogosCollection;

        public ParkLogosMongoQueryHandler(IMongoDatabase database, IMongoDbSettings settings)
        {
            parkLogosCollection = database.GetCollection<ParkLogo>(settings.ParkLogosCollectionName);
        }

        public async Task<ParkLogo?> GetByIdAsync(string id)
        {
            return await parkLogosCollection
                .Find(l => l.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<ParkLogo>> GetByParkIdAsync(string parkId)
        {
            return await parkLogosCollection
                .Find(l => l.ParkId == parkId)
                .SortByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<ParkLogo?> GetCurrentByParkIdAsync(string parkId)
        {
            return await parkLogosCollection
                .Find(l => l.ParkId == parkId && l.IsCurrent)
                .FirstOrDefaultAsync();
        }

        public async Task<ParkLogo> InsertAsync(ParkLogo logo)
        {
            await parkLogosCollection.InsertOneAsync(logo);
            return logo;
        }

        public async Task UpdateAsync(ParkLogo logo)
        {
            await parkLogosCollection.ReplaceOneAsync(
                l => l.Id == logo.Id,
                logo);
        }

        public async Task UnsetCurrentForParkAsync(string parkId, string? excludeLogoId = null)
        {
            var filter = Builders<ParkLogo>.Filter.Eq(l => l.ParkId, parkId);

            if (!string.IsNullOrWhiteSpace(excludeLogoId))
            {
                filter &= Builders<ParkLogo>.Filter.Ne(l => l.Id, excludeLogoId);
            }

            var update = Builders<ParkLogo>.Update
                .Set(l => l.IsCurrent, false);

            await parkLogosCollection.UpdateManyAsync(filter, update);
        }

        public async Task DeleteAsync(string id)
        {
            await parkLogosCollection.DeleteOneAsync(l => l.Id == id);
        }
    }
}