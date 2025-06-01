using Entities.Model.Parks;
using Entities.Model.Searching;
using MongoDB.Driver;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class SearchIndexService : ISearchIndexService
    {
        private readonly IMongoDatabase _database;

        public SearchIndexService(IMongoDatabase database)
        {
            _database = database;
        }

        /// <summary>
        /// Initialise la collection SearchItems à partir des documents Park.
        /// S’assure que la collection existe, crée les index, puis insère tous les SearchItem générés.
        /// </summary>
        public async Task InitializeFromParksAsync(IMongoDatabase database, string parksCollectionName, string searchItemCollectionName)
        {
            // 1) S’assurer que la collection SearchItems existe
            var filterColl = new MongoDB.Bson.BsonDocument("name", searchItemCollectionName);
            var collections = await database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filterColl });
            var exists = await collections.AnyAsync();
            if (!exists)
            {
                await database.CreateCollectionAsync(searchItemCollectionName);
            }

            // 2) Récupérer la collection Park et la collection SearchItem
            var parksColl = database.GetCollection<Park>(parksCollectionName);
            var searchColl = database.GetCollection<SearchItem>(searchItemCollectionName);

            // 3) Créer l’index géospatial sur SearchItems (sur le champ “location”)
            var geoIndexKeys = Builders<SearchItem>.IndexKeys.Geo2DSphere(x => x.Location);
            await searchColl.Indexes.CreateOneAsync(new CreateIndexModel<SearchItem>(geoIndexKeys));

            // 4) Créer l’index texte sur SearchItems (title, description, keywords)
            var textIndexKeys = Builders<SearchItem>.IndexKeys
                .Text(x => x.Title)
                .Text(x => x.Description)
                .Text(x => x.Keywords);
            var textIndexOptions = new CreateIndexOptions
            {
                DefaultLanguage = "french",
                Name = "Idx_SearchItem_Text"
            };
            await searchColl.Indexes.CreateOneAsync(new CreateIndexModel<SearchItem>(textIndexKeys, textIndexOptions));

            // 5) Extraire tous les Parks existants et générer une liste de SearchItems
            var allParks = await parksColl.Find(_ => true).ToListAsync();
            if (allParks == null || allParks.Count == 0)
            {
                return; // rien à insérer
            }

            var bulkOps = new List<WriteModel<SearchItem>>();

            foreach (var park in allParks)
            {
                var searchItem = ConvertParkToSearchItem(park);
                var filter = Builders<SearchItem>.Filter.Eq(si => si.OriginalId, searchItem.OriginalId);
                var upsertOne = new ReplaceOneModel<SearchItem>(filter, searchItem)
                {
                    IsUpsert = true
                };
                bulkOps.Add(upsertOne);
            }

            if (bulkOps.Count > 0)
            {
                // 6) BulkWrite pour gagner en performance si plusieurs centaines de docs
                await searchColl.BulkWriteAsync(bulkOps);
            }
        }

        /// <summary>
        /// Construit un SearchItem à partir d’un Park.
        /// On y inclut le nom, le code pays, la géolocalisation, etc.
        /// </summary>
        public SearchItem ConvertParkToSearchItem(Park park)
        {
            if (park == null) throw new ArgumentNullException(nameof(park));

            // On peut mettre “park_{id}” pour identifier l'origine
            var originalId = $"park_{park.Id}";

            // Keywords : on peut y mettre le nom du parc + le code pays
            var keywords = new List<string>();
            if (!string.IsNullOrWhiteSpace(park.Name))
                keywords.Add(park.Name.Trim().ToLowerInvariant());
            if (!string.IsNullOrWhiteSpace(park.CountryCode))
                keywords.Add(park.CountryCode.Trim().ToLowerInvariant());

            // Création du SearchItem
            var item = new SearchItem
            {
                // ModelBase (Id, CreatedAt, UpdatedAt) sera géré par MongoDB automatiquement
                OriginalId = originalId,
                Category = "park",
                Title = park.Name ?? string.Empty,
                Description = $"{park.Name} ({park.CountryCode})", // ou ajustez selon votre besoin
                Keywords = keywords,
                CompositeScore = 0.0, // si vous voulez un score par défaut
                Latitude = park.Latitude,
                Longitude = park.Longitude,
                // Location est remplie par GeolocatedEntity.UpdateLocation()
            };

            return item;
        }

        /// <summary>
        /// Upserte (insert ou update) le SearchItem correspondant dans la collection.
        /// </summary>
        public async Task UpsertSearchItemAsync(SearchItem item, string searchItemCollectionName)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var searchColl = _database.GetCollection<SearchItem>(searchItemCollectionName);
            var filter = Builders<SearchItem>.Filter.Eq(si => si.OriginalId, item.OriginalId);
            var options = new ReplaceOptions { IsUpsert = true };
            await searchColl.ReplaceOneAsync(filter, item, options);
        }

        /// <summary>
        /// Supprime un SearchItem d’après son originalId (ex. "park_...").
        /// </summary>
        public async Task DeleteSearchItemAsync(string originalId, string searchItemCollectionName)
        {
            if (string.IsNullOrWhiteSpace(originalId)) throw new ArgumentException("originalId cannot be null or whitespace.", nameof(originalId));

            var searchColl = _database.GetCollection<SearchItem>(searchItemCollectionName);
            var filter = Builders<SearchItem>.Filter.Eq(si => si.OriginalId, originalId);
            await searchColl.DeleteOneAsync(filter);
        }
    }
}
