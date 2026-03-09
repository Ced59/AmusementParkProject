using Entities.Model.Parks;
using MongoDB.Bson;
using MongoDB.Driver;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class ParkItemsMongoQueryHandler : IParkItemsQueryHandler
    {
        private readonly IMongoCollection<ParkItem> itemsCollection;

        public ParkItemsMongoQueryHandler(IMongoDatabase database, IMongoDbSettings settings)
        {
            itemsCollection = database.GetCollection<ParkItem>(settings.ParkItemsCollectionName);
        }

        public async Task<IEnumerable<ParkItem>> GetByParkIdAsync(string parkId, bool includeNonVisible = true)
        {
            FilterDefinition<ParkItem> filter = Builders<ParkItem>.Filter.Eq(item => item.ParkId, parkId);

            if (!includeNonVisible)
            {
                filter &= Builders<ParkItem>.Filter.Eq(item => item.IsVisible, true);
            }

            return await itemsCollection.Find(filter)
                .SortBy(item => item.Category)
                .ThenBy(item => item.Type)
                .ThenBy(item => item.Name)
                .ToListAsync();
        }

        public async Task<ParkItem?> GetByIdAsync(string id)
        {
            return await itemsCollection.Find(item => item.Id == id).FirstOrDefaultAsync();
        }

        public async Task<ParkItem?> CreateAsync(ParkItem item)
        {
            try
            {
                await itemsCollection.InsertOneAsync(item);
                return item;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ParkItem?> UpdateAsync(ParkItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                return null;
            }

            ReplaceOneResult result = await itemsCollection.ReplaceOneAsync(existing => existing.Id == item.Id, item);
            return result.MatchedCount == 0 ? null : item;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            DeleteResult result = await itemsCollection.DeleteOneAsync(item => item.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<long> ClearZoneAsync(string zoneId)
        {
            UpdateResult result = await itemsCollection.UpdateManyAsync(
                item => item.ZoneId == zoneId,
                Builders<ParkItem>.Update
                    .Set(item => item.ZoneId, null)
                    .Set(item => item.UpdatedAt, DateTime.UtcNow));

            return result.ModifiedCount;
        }

        public async Task<Dictionary<string, int>> GetAttractionCountsByManufacturerIdsAsync(IEnumerable<string> manufacturerIds)
        {
            List<string> ids = manufacturerIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (ids.Count == 0)
            {
                return new Dictionary<string, int>(StringComparer.Ordinal);
            }

            FilterDefinition<ParkItem> filter =
                Builders<ParkItem>.Filter.Eq(item => item.Category, ParkItemCategory.Attraction) &
                Builders<ParkItem>.Filter.In("attractionDetails.manufacturerId", ids);

            List<BsonDocument> aggregationResults = await itemsCollection.Aggregate()
                .Match(filter)
                .Group(new BsonDocument
                {
                    { "_id", "$attractionDetails.manufacturerId" },
                    { "count", new BsonDocument("$sum", 1) }
                })
                .ToListAsync();

            Dictionary<string, int> counts = new(StringComparer.Ordinal);

            foreach (BsonDocument document in aggregationResults)
            {
                BsonValue manufacturerIdValue = document.GetValue("_id", BsonNull.Value);
                if (!manufacturerIdValue.IsString)
                {
                    continue;
                }

                string manufacturerId = manufacturerIdValue.AsString;
                int count = document.GetValue("count", 0).ToInt32();

                if (!string.IsNullOrWhiteSpace(manufacturerId))
                {
                    counts[manufacturerId] = count;
                }
            }

            return counts;
        }
    }
}
