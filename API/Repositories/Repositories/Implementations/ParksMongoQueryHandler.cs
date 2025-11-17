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
            FilterDefinition<Park>? filter = includeNonVisible
                ? Builders<Park>.Filter.Empty
                : Builders<Park>.Filter.Eq(p => p.IsVisible, true);

            return await parksCollection.Find(filter)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<long> GetTotalParksCountAsync(bool includeNonVisible = false)
        {
            FilterDefinition<Park>? filter = includeNonVisible
                ? Builders<Park>.Filter.Empty
                : Builders<Park>.Filter.Eq(p => p.IsVisible, true);

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
            FilterDefinition<Park>? geoFilter = Builders<Park>.Filter.NearSphere(
                p => p.Location,
                new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                    new GeoJson2DGeographicCoordinates(longitude, latitude)),
                maxDistance: maxDistanceInMeters
            );

            // Filtre de visibilité publique
            FilterDefinition<Park>? visibleFilter = Builders<Park>.Filter.Eq(p => p.IsVisible, true);

            // 🔹 On combine les deux : proche + visible
            FilterDefinition<Park>? filter = Builders<Park>.Filter.And(geoFilter, visibleFilter);

            List<Park>? parks = await parksCollection
                .Find(filter)
                .ToListAsync();

            return parks;
        }


        public async Task<long> GetTotalParksCountByNameAsync(
            string name,
            bool includeNonVisible = false)
        {
            FilterDefinition<Park>? nameFilter = Builders<Park>.Filter.Regex(
                p => p.Name,
                new BsonRegularExpression(name, "i"));

            FilterDefinition<Park>? filter = nameFilter;

            if (!includeNonVisible)
            {
                FilterDefinition<Park>? visibleFilter = Builders<Park>.Filter.Eq(p => p.IsVisible, true);
                filter = filter & visibleFilter;
            }

            return await parksCollection.CountDocumentsAsync(filter);
        }

        public async Task<IEnumerable<Park>> GetParksByNamePaginatedAsync(
            string name,
            int page,
            int pageSize,
            bool includeNonVisible = false)
        {
            FilterDefinition<Park>? nameFilter = Builders<Park>.Filter.Regex(
                p => p.Name,
                new BsonRegularExpression(name, "i"));

            FilterDefinition<Park>? filter = nameFilter;

            if (!includeNonVisible)
            {
                FilterDefinition<Park>? visibleFilter = Builders<Park>.Filter.Eq(p => p.IsVisible, true);
                filter = filter & visibleFilter;
            }

            return await parksCollection.Find(filter)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }


        public async Task<Park?> UpdateParkVisibilityAsync(string id, bool isVisible)
        {
            FilterDefinition<Park>? filter = Builders<Park>.Filter.Eq(p => p.Id, id);

            UpdateDefinition<Park>? update = Builders<Park>.Update
                .Set(p => p.IsVisible, isVisible)
                .Set(p => p.UpdatedAt, DateTime.UtcNow);

            FindOneAndUpdateOptions<Park> options = new FindOneAndUpdateOptions<Park>
            {
                ReturnDocument = ReturnDocument.After // on récupère la version après update
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

            FilterDefinition<Park> filter = Builders<Park>.Filter.Eq(p => p.Id, park.Id);

            ReplaceOneResult result = await parksCollection.ReplaceOneAsync(filter, park);

            if (result.MatchedCount == 0)
            {
                return null;
            }

            // On renvoie l'instance modifiée en mémoire (elle est déjà à jour)
            return park;
        }
    }
}