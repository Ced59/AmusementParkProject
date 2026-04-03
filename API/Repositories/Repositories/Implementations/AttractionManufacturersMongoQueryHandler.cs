using System.Collections.Generic;
using System.Threading.Tasks;
using Entities.Model.Parks;
using MongoDB.Driver;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class AttractionManufacturersMongoQueryHandler : IAttractionManufacturersQueryHandler
    {
        private readonly IMongoCollection<AttractionManufacturer> manufacturersCollection;

        public AttractionManufacturersMongoQueryHandler(IMongoDatabase database, IMongoDbSettings settings)
        {
            manufacturersCollection = database.GetCollection<AttractionManufacturer>(settings.AttractionManufacturersCollectionName);
        }

        public async Task<IEnumerable<AttractionManufacturer>> GetAllAsync()
        {
            return await manufacturersCollection.Find(Builders<AttractionManufacturer>.Filter.Empty)
                .SortBy(manufacturer => manufacturer.Name)
                .ToListAsync();
        }

        public async Task<AttractionManufacturer?> GetByIdAsync(string id)
        {
            return await manufacturersCollection.Find(manufacturer => manufacturer.Id == id).FirstOrDefaultAsync();
        }

        public async Task<AttractionManufacturer?> CreateAsync(AttractionManufacturer manufacturer)
        {
            try
            {
                await manufacturersCollection.InsertOneAsync(manufacturer);
                return manufacturer;
            }
            catch
            {
                return null;
            }
        }

        public async Task<AttractionManufacturer?> UpdateAsync(AttractionManufacturer manufacturer)
        {
            if (string.IsNullOrWhiteSpace(manufacturer.Id))
            {
                return null;
            }

            ReplaceOneResult result = await manufacturersCollection.ReplaceOneAsync(
                existing => existing.Id == manufacturer.Id,
                manufacturer);

            if (result.MatchedCount == 0)
            {
                return null;
            }

            return manufacturer;
        }
    }
}
