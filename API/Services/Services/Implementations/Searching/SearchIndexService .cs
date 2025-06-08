using Entities.Model.Parks;
using Entities.Model.Searching;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using Services.Interfaces.Searching;

namespace Services.Implementations.Searching
{
    public class SearchIndexService : ISearchIndexService
    {
        private readonly IMongoDatabase database;

        public SearchIndexService(IMongoDatabase database)
        {
            this.database = database;
        }

        /// <summary>
        /// Initialise la collection SearchItems à partir de tous les documents Park existants,
        /// en utilisant des opérations "UpdateOne" avec upsert pour ne jamais altérer _id.
        /// </summary>
        public async Task InitializeFromParksAsync(
            IMongoDatabase database,
            string parksCollectionName,
            string searchItemCollectionName)
        {
            // 1) S’assurer que la collection SearchItems existe
            BsonDocument filterColl = new("name", searchItemCollectionName);
            IAsyncCursor<BsonDocument>? collections = await database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filterColl });
            bool exists = await collections.AnyAsync();
            if (!exists)
            {
                await database.CreateCollectionAsync(searchItemCollectionName);
            }

            // 2) Références aux collections Parks et SearchItems
            IMongoCollection<Park>? parksColl = database.GetCollection<Park>(parksCollectionName);
            IMongoCollection<SearchItem>? searchColl = database.GetCollection<SearchItem>(searchItemCollectionName);

            // 3) Créer les index s’ils n’existent pas encore
            //    - Geo index 2dsphere sur "location"
            IndexKeysDefinition<SearchItem>? geoIndexKeys = Builders<SearchItem>.IndexKeys.Geo2DSphere(x => x.Location);
            await searchColl.Indexes.CreateOneAsync(new CreateIndexModel<SearchItem>(geoIndexKeys));

            //    - Text index sur (title, description, keywords)
            IndexKeysDefinition<SearchItem>? textIndexKeys = Builders<SearchItem>.IndexKeys
                .Text(x => x.Title)
                .Text(x => x.Description)
                .Text(x => x.Keywords);
            CreateIndexOptions textIndexOptions = new()
            {
                DefaultLanguage = "french",
                Name = "Idx_SearchItem_Text"
            };
            await searchColl.Indexes.CreateOneAsync(new CreateIndexModel<SearchItem>(textIndexKeys, textIndexOptions));

            // 4) Lire tous les Parks existants
            List<Park>? allParks = await parksColl.Find(_ => true).ToListAsync();
            if (allParks == null || allParks.Count == 0)
            {
                return; // rien à insérer
            }

            // 5) Préparer les opérations bulk (UpdateOneModel avec Upsert)
            List<WriteModel<SearchItem>> bulkOps = new();

            foreach (Park park in allParks)
            {
                // 5.1) Construire OriginalId
                string originalId = $"park_{park.Id}";

                // 5.2) Créer le filtre sur OriginalId (non modifiable)
                FilterDefinition<SearchItem>? filter = Builders<SearchItem>.Filter.Eq(si => si.OriginalId, originalId);

                // 5.3) Construire les champs à mettre à jour
                //      - On met à jour category, title, description, keywords, latitude, longitude, updatedAt.
                //      - Si on insère (upsert), on souhaite définir createdAt avec la date actuelle Windows UTC.
                UpdateDefinition<SearchItem>? update = Builders<SearchItem>.Update
                    .Set(si => si.Category, "park")
                    .Set(si => si.Title, park.Name ?? string.Empty)
                    .Set(si => si.Description, $"{park.Name} ({park.CountryCode})")
                    .Set(si => si.Keywords, new List<string>
                    {
                        park.Name?.Trim().ToLowerInvariant() ?? string.Empty,
                        park.CountryCode?.Trim().ToLowerInvariant() ?? string.Empty
                    })
                    .Set(si => si.Latitude, park.Latitude)
                    .Set(si => si.Longitude, park.Longitude)
                    .Set(si => si.UpdatedAt, park.UpdatedAt)
                    // Si on souhaite remplir CreatedAt seulement lors d'un insert
                    .SetOnInsert(si => si.CreatedAt, DateTime.UtcNow);

                // 5.4) Conserver aussi la géolocalisation (Location) via Set(si => si.Location, …)
                //      Mais GeoJsonPoint est généré automatiquement dans le setter de Latitude/Longitude, 
                //      donc ce Set(n’est pas forcément nécessaire si les propriétés lat/long s’écrivent)
                update = update.Set(si => si.Location,
                    new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                        new GeoJson2DGeographicCoordinates(park.Longitude, park.Latitude)));

                // 5.5) Construire le modèle Bulk : UpdateOneModel avec Upsert = true
                UpdateOneModel<SearchItem> upsertOne = new(filter, update)
                {
                    IsUpsert = true
                };

                bulkOps.Add(upsertOne);
            }

            // 6) Exécuter le bulk write en une seule fois
            if (bulkOps.Count > 0)
            {
                await searchColl.BulkWriteAsync(bulkOps);
            }
        }

        /// <summary>
        /// Convertit un Park en SearchItem (pour appels ultérieurs Create/Update individuels).
        /// </summary>
        public SearchItem ConvertParkToSearchItem(Park park)
        {
            if (park == null) throw new ArgumentNullException(nameof(park));

            string originalId = $"park_{park.Id}";
            List<string> keywords = new();
            if (!string.IsNullOrWhiteSpace(park.Name))
                keywords.Add(park.Name.Trim().ToLowerInvariant());
            if (!string.IsNullOrWhiteSpace(park.CountryCode))
                keywords.Add(park.CountryCode.Trim().ToLowerInvariant());

            SearchItem item = new()
            {
                OriginalId = originalId,
                Category = "park",
                Title = park.Name ?? string.Empty,
                Description = $"{park.Name} ({park.CountryCode})",
                Keywords = keywords,
                CompositeScore = 0.0,
                Latitude = park.Latitude,
                Longitude = park.Longitude,
                CreatedAt = park.CreatedAt,
                UpdatedAt = park.UpdatedAt
            };
            return item;
        }

        public async Task UpsertSearchItemAsync(SearchItem item, string searchItemCollectionName)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            IMongoCollection<SearchItem>? searchColl = database.GetCollection<SearchItem>(searchItemCollectionName);
            FilterDefinition<SearchItem>? filter = Builders<SearchItem>.Filter.Eq(si => si.OriginalId, item.OriginalId);

            // Construire un UpdateDefinition similaire à InitializeFromParksAsync
            UpdateDefinition<SearchItem>? update = Builders<SearchItem>.Update
                .Set(si => si.Category, item.Category)
                .Set(si => si.Title, item.Title)
                .Set(si => si.Description, item.Description)
                .Set(si => si.Keywords, item.Keywords)
                .Set(si => si.Latitude, item.Latitude)
                .Set(si => si.Longitude, item.Longitude)
                .Set(si => si.UpdatedAt, item.UpdatedAt)
                .SetOnInsert(si => si.CreatedAt, item.CreatedAt)
                .Set(si => si.CompositeScore, item.CompositeScore)
                .Set(si => si.Location, new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                    new GeoJson2DGeographicCoordinates(item.Longitude, item.Latitude)));

            UpdateOptions options = new() { IsUpsert = true };
            await searchColl.UpdateOneAsync(filter, update, options);
        }

        public async Task DeleteSearchItemAsync(string originalId, string searchItemCollectionName)
        {
            if (string.IsNullOrWhiteSpace(originalId)) throw new ArgumentException(nameof(originalId));

            IMongoCollection<SearchItem>? searchColl = database.GetCollection<SearchItem>(searchItemCollectionName);
            FilterDefinition<SearchItem>? filter = Builders<SearchItem>.Filter.Eq(si => si.OriginalId, originalId);
            await searchColl.DeleteOneAsync(filter);
        }
    }
}