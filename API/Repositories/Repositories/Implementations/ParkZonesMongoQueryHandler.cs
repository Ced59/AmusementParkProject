using Entities.Model.Parks;
using MongoDB.Driver;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class ParkZonesMongoQueryHandler : IParkZonesQueryHandler
    {
        private readonly IMongoCollection<ParkZone> zonesCollection;

        public ParkZonesMongoQueryHandler(IMongoDatabase database, IMongoDbSettings settings)
        {
            zonesCollection = database.GetCollection<ParkZone>(settings.ParkZonesCollectionName);
        }

        public async Task<IEnumerable<ParkZone>> GetByParkIdAsync(string parkId)
        {
            return await zonesCollection
                .Find(zone => zone.ParkId == parkId)
                .SortBy(zone => zone.SortOrder)
                .ThenBy(zone => zone.Name)
                .ThenBy(zone => zone.Id)
                .ToListAsync();
        }

        public async Task<ParkZone?> GetByIdAsync(string id)
        {
            return await zonesCollection.Find(zone => zone.Id == id).FirstOrDefaultAsync();
        }

        public async Task<ParkZone?> CreateAsync(ParkZone zone)
        {
            try
            {
                await zonesCollection.InsertOneAsync(zone);
                return zone;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ParkZone?> UpdateAsync(ParkZone zone)
        {
            if (string.IsNullOrWhiteSpace(zone.Id))
            {
                return null;
            }

            ReplaceOneResult result = await zonesCollection.ReplaceOneAsync(item => item.Id == zone.Id, zone);
            return result.MatchedCount == 0 ? null : zone;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            DeleteResult result = await zonesCollection.DeleteOneAsync(zone => zone.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
