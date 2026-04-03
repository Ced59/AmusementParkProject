using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Entities.Model.Parks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class ParksMongoQueryHandler : IParksQueryHandler
    {
        private readonly IMongoCollection<Park> parksCollection;

        public ParksMongoQueryHandler(IMongoDatabase database, IMongoDbSettings settings)
        {
            parksCollection = database.GetCollection<Park>(settings.ParksCollectionName);
        }

        public async Task<Park?> GetParkByIdAsync(string id)
        {
            return await parksCollection.Find(park => park.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Park>> GetParksPaginatedAsync(
            int page,
            int pageSize,
            bool includeNonVisible = false)
        {
            FilterDefinition<Park> filter = includeNonVisible
                ? Builders<Park>.Filter.Empty
                : Builders<Park>.Filter.Eq(park => park.IsVisible, true);

            return await parksCollection.Find(filter)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Park>> GetParksByIdsAsync(IEnumerable<string> ids)
        {
            List<string> normalizedIds = ids
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (normalizedIds.Count == 0)
            {
                return new List<Park>();
            }

            FilterDefinition<Park> filter = Builders<Park>.Filter.In(park => park.Id, normalizedIds);

            return await parksCollection.Find(filter).ToListAsync();
        }

        public async Task<long> GetTotalParksCountAsync(bool includeNonVisible = false)
        {
            FilterDefinition<Park> filter = includeNonVisible
                ? Builders<Park>.Filter.Empty
                : Builders<Park>.Filter.Eq(park => park.IsVisible, true);

            return await parksCollection.CountDocumentsAsync(filter);
        }

        public async Task<Park?> CreateParkAsync(Park park)
        {
            try
            {
                await parksCollection.InsertOneAsync(park);
                return park;
            }
            catch
            {
                return null;
            }
        }

        public async Task<IEnumerable<Park>> GetParksByLocationAsync(
            double latitude,
            double longitude,
            double maxDistanceInMeters)
        {
            FilterDefinition<Park> geoFilter = Builders<Park>.Filter.NearSphere(
                park => park.Location,
                new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                    new GeoJson2DGeographicCoordinates(longitude, latitude)),
                maxDistance: maxDistanceInMeters);

            FilterDefinition<Park> visibleFilter = Builders<Park>.Filter.Eq(park => park.IsVisible, true);
            FilterDefinition<Park> filter = Builders<Park>.Filter.And(geoFilter, visibleFilter);

            List<Park> parks = await parksCollection.Find(filter).ToListAsync();
            return parks;
        }

        public async Task<long> GetTotalParksCountByNameAsync(
            string name,
            bool includeNonVisible = false)
        {
            FilterDefinition<Park> nameFilter = Builders<Park>.Filter.Regex(
                park => park.Name,
                new BsonRegularExpression(name, "i"));

            FilterDefinition<Park> filter = nameFilter;

            if (!includeNonVisible)
            {
                FilterDefinition<Park> visibleFilter = Builders<Park>.Filter.Eq(park => park.IsVisible, true);
                filter &= visibleFilter;
            }

            return await parksCollection.CountDocumentsAsync(filter);
        }

        public async Task<IEnumerable<Park>> GetParksByNamePaginatedAsync(
            string name,
            int page,
            int pageSize,
            bool includeNonVisible = false)
        {
            FilterDefinition<Park> nameFilter = Builders<Park>.Filter.Regex(
                park => park.Name,
                new BsonRegularExpression(name, "i"));

            FilterDefinition<Park> filter = nameFilter;

            if (!includeNonVisible)
            {
                FilterDefinition<Park> visibleFilter = Builders<Park>.Filter.Eq(park => park.IsVisible, true);
                filter &= visibleFilter;
            }

            return await parksCollection.Find(filter)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<Park?> UpdateParkVisibilityAsync(string id, bool isVisible)
        {
            FilterDefinition<Park> filter = Builders<Park>.Filter.Eq(park => park.Id, id);

            UpdateDefinition<Park> update = Builders<Park>.Update
                .Set(park => park.IsVisible, isVisible)
                .Set(park => park.UpdatedAt, DateTime.UtcNow);

            FindOneAndUpdateOptions<Park> options = new()
            {
                ReturnDocument = ReturnDocument.After
            };

            Park? updated = await parksCollection.FindOneAndUpdateAsync(filter, update, options);
            return updated;
        }

        public async Task<Park?> UpdateParkAsync(Park park)
        {
            if (string.IsNullOrWhiteSpace(park.Id))
            {
                return null;
            }

            FilterDefinition<Park> filter = Builders<Park>.Filter.Eq(existingPark => existingPark.Id, park.Id);
            ReplaceOneResult result = await parksCollection.ReplaceOneAsync(filter, park);

            if (result.MatchedCount == 0)
            {
                return null;
            }

            return park;
        }

        public async Task<bool> UpdateCurrentLogoAsync(string parkId, string? logoImageId)
        {
            FilterDefinition<Park> filter = Builders<Park>.Filter.Eq(park => park.Id, parkId);
            UpdateDefinition<Park> update = Builders<Park>.Update
                .Set(park => park.CurrentLogoImageId, logoImageId)
                .Set(park => park.UpdatedAt, DateTime.UtcNow);

            UpdateResult result = await parksCollection.UpdateOneAsync(filter, update);
            return result.MatchedCount > 0;
        }
    }
}
