using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.General.Localization;
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
        private static readonly string[] SupportedCategories = new[]
        {
            "park",
            "parkItems",
            "operators",
            "manufacturers"
        };

        private readonly IMongoDatabase database;

        public SearchIndexService(IMongoDatabase database)
        {
            this.database = database;
        }

        public async Task InitializeAsync(
            IMongoDatabase database,
            string parksCollectionName,
            string parkItemsCollectionName,
            string parkOperatorsCollectionName,
            string attractionManufacturersCollectionName,
            string searchItemCollectionName)
        {
            IMongoCollection<SearchItem> searchCollection = await EnsureSearchCollectionAsync(database, searchItemCollectionName);

            long existingCount = await searchCollection.EstimatedDocumentCountAsync();
            if (existingCount > 0)
            {
                return;
            }

            IMongoCollection<Park> parksCollection = database.GetCollection<Park>(parksCollectionName);
            IMongoCollection<ParkItem> parkItemsCollection = database.GetCollection<ParkItem>(parkItemsCollectionName);
            IMongoCollection<ParkOperator> operatorsCollection = database.GetCollection<ParkOperator>(parkOperatorsCollectionName);
            IMongoCollection<AttractionManufacturer> manufacturersCollection = database.GetCollection<AttractionManufacturer>(attractionManufacturersCollectionName);

            List<Park> parks = await parksCollection.Find(_ => true).ToListAsync();
            List<ParkItem> parkItems = await parkItemsCollection.Find(_ => true).ToListAsync();
            List<ParkOperator> operators = await operatorsCollection.Find(_ => true).ToListAsync();
            List<AttractionManufacturer> manufacturers = await manufacturersCollection.Find(_ => true).ToListAsync();

            Dictionary<string, Park> parksById = parks
                .Where(park => !string.IsNullOrWhiteSpace(park.Id))
                .ToDictionary(park => park.Id!, park => park, StringComparer.Ordinal);

            List<SearchItem> itemsToUpsert = new();
            itemsToUpsert.AddRange(parks.Select(ConvertParkToSearchItem));
            itemsToUpsert.AddRange(parkItems.Select(parkItem =>
            {
                Park? park = parksById.TryGetValue(parkItem.ParkId, out Park? resolvedPark) ? resolvedPark : null;
                SearchItem searchItem = ConvertParkItemToSearchItem(parkItem, park?.Name ?? string.Empty);
                searchItem.IsVisible = (park?.IsVisible ?? true) && parkItem.IsVisible;
                return searchItem;
            }));
            itemsToUpsert.AddRange(operators.Select(ConvertParkOperatorToSearchItem));
            itemsToUpsert.AddRange(manufacturers.Select(ConvertAttractionManufacturerToSearchItem));

            if (itemsToUpsert.Count == 0)
            {
                return;
            }

            List<WriteModel<SearchItem>> bulkOperations = itemsToUpsert
                .Select(BuildUpsertModel)
                .Cast<WriteModel<SearchItem>>()
                .ToList();

            await searchCollection.BulkWriteAsync(bulkOperations);
        }

        public SearchItem ConvertParkToSearchItem(Park park)
        {
            if (park == null)
            {
                throw new ArgumentNullException(nameof(park));
            }

            List<string> keywords = new();
            AddKeyword(keywords, park.Name);
            AddKeyword(keywords, park.CountryCode);
            AddKeyword(keywords, park.City);
            AddKeyword(keywords, park.PostalCode);
            AddKeyword(keywords, park.Type?.ToString());

            return new SearchItem
            {
                OriginalId = $"park_{park.Id}",
                Category = "park",
                Title = park.Name ?? string.Empty,
                Description = ResolveLocalizedText(park.Descriptions) ?? BuildParkFallbackDescription(park),
                Keywords = keywords,
                CompositeScore = 0.0,
                Latitude = park.Latitude,
                Longitude = park.Longitude,
                CreatedAt = park.CreatedAt,
                UpdatedAt = park.UpdatedAt,
                IsVisible = park.IsVisible
            };
        }

        public SearchItem ConvertParkItemToSearchItem(ParkItem parkItem, string parkName)
        {
            if (parkItem == null)
            {
                throw new ArgumentNullException(nameof(parkItem));
            }

            List<string> keywords = new();
            AddKeyword(keywords, parkItem.Name);
            AddKeyword(keywords, parkItem.Subtype);
            AddKeyword(keywords, parkItem.Type.ToString());
            AddKeyword(keywords, parkItem.Category.ToString());
            AddKeyword(keywords, parkName);
            AddKeyword(keywords, parkItem.AttractionDetails?.Model);

            string fallbackDescription = !string.IsNullOrWhiteSpace(parkName)
                ? $"{parkName} • {parkItem.Type}"
                : parkItem.Type.ToString();

            return new SearchItem
            {
                OriginalId = $"parkItem_{parkItem.Id}",
                Category = "parkItems",
                Title = parkItem.Name,
                Description = ResolveLocalizedText(parkItem.Descriptions) ?? fallbackDescription,
                Keywords = keywords,
                CompositeScore = 0.0,
                Latitude = parkItem.Latitude,
                Longitude = parkItem.Longitude,
                CreatedAt = parkItem.CreatedAt,
                UpdatedAt = parkItem.UpdatedAt,
                IsVisible = parkItem.IsVisible
            };
        }

        public SearchItem ConvertParkOperatorToSearchItem(ParkOperator parkOperator)
        {
            if (parkOperator == null)
            {
                throw new ArgumentNullException(nameof(parkOperator));
            }

            List<string> keywords = new();
            AddKeyword(keywords, parkOperator.Name);
            AddKeyword(keywords, "operator");

            return new SearchItem
            {
                OriginalId = $"operator_{parkOperator.Id}",
                Category = "operators",
                Title = parkOperator.Name,
                Description = ResolveLocalizedText(parkOperator.Description) ?? parkOperator.Name,
                Keywords = keywords,
                CompositeScore = 0.0,
                Latitude = 0.0,
                Longitude = 0.0,
                CreatedAt = parkOperator.CreatedAt,
                UpdatedAt = parkOperator.UpdatedAt,
                IsVisible = true
            };
        }

        public SearchItem ConvertAttractionManufacturerToSearchItem(AttractionManufacturer manufacturer)
        {
            if (manufacturer == null)
            {
                throw new ArgumentNullException(nameof(manufacturer));
            }

            List<string> keywords = new();
            AddKeyword(keywords, manufacturer.Name);
            AddKeyword(keywords, "manufacturer");
            AddKeyword(keywords, "constructor");

            return new SearchItem
            {
                OriginalId = $"manufacturer_{manufacturer.Id}",
                Category = "manufacturers",
                Title = manufacturer.Name,
                Description = ResolveLocalizedText(manufacturer.Biography) ?? manufacturer.Name,
                Keywords = keywords,
                CompositeScore = 0.0,
                Latitude = 0.0,
                Longitude = 0.0,
                CreatedAt = manufacturer.CreatedAt,
                UpdatedAt = manufacturer.UpdatedAt,
                IsVisible = true
            };
        }

        public async Task UpsertSearchItemAsync(SearchItem item, string searchItemCollectionName)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            IMongoCollection<SearchItem> searchCollection = database.GetCollection<SearchItem>(searchItemCollectionName);
            FilterDefinition<SearchItem> filter = Builders<SearchItem>.Filter.Eq(searchItem => searchItem.OriginalId, item.OriginalId);
            UpdateDefinition<SearchItem> update = BuildUpdateDefinition(item);
            UpdateOptions options = new() { IsUpsert = true };
            await searchCollection.UpdateOneAsync(filter, update, options);
        }

        public async Task DeleteSearchItemAsync(string originalId, string searchItemCollectionName)
        {
            if (string.IsNullOrWhiteSpace(originalId))
            {
                throw new ArgumentException(nameof(originalId));
            }

            IMongoCollection<SearchItem> searchCollection = database.GetCollection<SearchItem>(searchItemCollectionName);
            FilterDefinition<SearchItem> filter = Builders<SearchItem>.Filter.Eq(searchItem => searchItem.OriginalId, originalId);
            await searchCollection.DeleteOneAsync(filter);
        }

        private static async Task<IMongoCollection<SearchItem>> EnsureSearchCollectionAsync(IMongoDatabase database, string searchItemCollectionName)
        {
            BsonDocument collectionFilter = new("name", searchItemCollectionName);
            IAsyncCursor<BsonDocument> collections = await database.ListCollectionsAsync(new ListCollectionsOptions { Filter = collectionFilter });
            bool exists = await collections.AnyAsync();
            if (!exists)
            {
                await database.CreateCollectionAsync(searchItemCollectionName);
            }

            IMongoCollection<SearchItem> searchCollection = database.GetCollection<SearchItem>(searchItemCollectionName);
            IndexKeysDefinition<SearchItem> geoIndexKeys = Builders<SearchItem>.IndexKeys.Geo2DSphere(item => item.Location);
            await searchCollection.Indexes.CreateOneAsync(new CreateIndexModel<SearchItem>(geoIndexKeys));

            IndexKeysDefinition<SearchItem> textIndexKeys = Builders<SearchItem>.IndexKeys
                .Text(item => item.Title)
                .Text(item => item.Description)
                .Text(item => item.Keywords);

            CreateIndexOptions textIndexOptions = new()
            {
                DefaultLanguage = "french",
                Name = "Idx_SearchItem_Text"
            };

            await searchCollection.Indexes.CreateOneAsync(new CreateIndexModel<SearchItem>(textIndexKeys, textIndexOptions));

            return searchCollection;
        }

        private static UpdateOneModel<SearchItem> BuildUpsertModel(SearchItem item)
        {
            FilterDefinition<SearchItem> filter = Builders<SearchItem>.Filter.Eq(searchItem => searchItem.OriginalId, item.OriginalId);
            UpdateDefinition<SearchItem> update = BuildUpdateDefinition(item);
            return new UpdateOneModel<SearchItem>(filter, update)
            {
                IsUpsert = true
            };
        }

        private static UpdateDefinition<SearchItem> BuildUpdateDefinition(SearchItem item)
        {
            return Builders<SearchItem>.Update
                .Set(searchItem => searchItem.Category, item.Category)
                .Set(searchItem => searchItem.Title, item.Title)
                .Set(searchItem => searchItem.Description, item.Description)
                .Set(searchItem => searchItem.Keywords, item.Keywords)
                .Set(searchItem => searchItem.Latitude, item.Latitude)
                .Set(searchItem => searchItem.Longitude, item.Longitude)
                .Set(searchItem => searchItem.CompositeScore, item.CompositeScore)
                .Set(searchItem => searchItem.UpdatedAt, item.UpdatedAt)
                .Set(searchItem => searchItem.IsVisible, item.IsVisible)
                .Set(searchItem => searchItem.Location, new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                    new GeoJson2DGeographicCoordinates(item.Longitude, item.Latitude)))
                .SetOnInsert(searchItem => searchItem.Id, string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString() : item.Id)
                .SetOnInsert(searchItem => searchItem.CreatedAt, item.CreatedAt)
                .SetOnInsert(searchItem => searchItem.OriginalId, item.OriginalId);
        }

        private static string? ResolveLocalizedText(IEnumerable<LocalizedItem<string>>? items)
        {
            if (items == null)
            {
                return null;
            }

            return items.Resolve("en", "fr");
        }

        private static string BuildParkFallbackDescription(Park park)
        {
            List<string> parts = new();
            if (!string.IsNullOrWhiteSpace(park.City))
            {
                parts.Add(park.City.Trim());
            }

            if (!string.IsNullOrWhiteSpace(park.CountryCode))
            {
                parts.Add(park.CountryCode.Trim().ToUpperInvariant());
            }

            return parts.Count > 0 ? string.Join(" • ", parts) : (park.Name ?? string.Empty);
        }

        private static void AddKeyword(List<string> keywords, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            string normalizedValue = value.Trim().ToLowerInvariant();
            if (!keywords.Contains(normalizedValue, StringComparer.Ordinal))
            {
                keywords.Add(normalizedValue);
            }
        }
    }
}
