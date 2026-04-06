using System;
using System.Collections.Generic;
using Entities.Model.Parks;
using MongoDB.Bson;
using MongoDB.Driver;
using Repositories.Interfaces;
using Services.Interfaces.Searching;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Entities.Model.Countries;
using WebAPI.Resources.InitializingDatas;

namespace WebAPI.Settings.MongoDB
{
    public static class MongoDbInitializer
    {
        public static async Task InitializeCollectionsAsync(
            IMongoDatabase database,
            IMongoDbSettings settings,
            ISearchIndexService searchIndexService)
        {
            // Collections "techniques"
            await EnsureCollectionExistsAsync(database, settings.UsersCollectionName);
            await EnsureCollectionExistsAsync(database, settings.ImagesCollectionName);
            await InitializeImagesIndexesAsync(database, settings.ImagesCollectionName);
            await EnsureCollectionExistsAsync(database, settings.ImageTagsCollectionName);
            await InitializeImageTagsIndexesAsync(database, settings.ImageTagsCollectionName);

            // 🔹 Countries
            await EnsureCollectionExistsAsync(database, settings.CountriesCollectionName);
            await InitializeCountriesCollection(
                database,
                settings.CountriesCollectionName,
                GetSeedFilePath("countries.seed.json")
            );

            // 🔹 Parks
            await EnsureCollectionExistsAsync(database, settings.ParksCollectionName);
            await InitializeParksCollection(
                database,
                settings.ParksCollectionName,
                GetSeedFilePath("parks.json")
            );

            // 🔹 Park founders / operators
            await EnsureCollectionExistsAsync(database, settings.ParkFoundersCollectionName);
            await EnsureCollectionExistsAsync(database, settings.ParkOperatorsCollectionName);
            await EnsureCollectionExistsAsync(database, settings.AttractionManufacturersCollectionName);
            await EnsureCollectionExistsAsync(database, settings.ParkZonesCollectionName);
            await EnsureCollectionExistsAsync(database, settings.ParkItemsCollectionName);
            await InitializeParkItemsIndexesAsync(database, settings.ParkItemsCollectionName);
            await EnsureCollectionExistsAsync(database, settings.CaptainCoasterSettingsCollectionName);
            await EnsureCollectionExistsAsync(database, settings.CaptainCoasterParksCollectionName);
            await EnsureCollectionExistsAsync(database, settings.CaptainCoasterCoastersCollectionName);
            await EnsureCollectionExistsAsync(database, settings.CaptainCoasterSyncSessionsCollectionName);
            await EnsureCollectionExistsAsync(database, settings.CaptainCoasterComparisonResultsCollectionName);

            // 🔹 Index de recherche
            await searchIndexService.InitializeAsync(
                database,
                settings.ParksCollectionName,
                settings.ParkItemsCollectionName,
                settings.ParkOperatorsCollectionName,
                settings.AttractionManufacturersCollectionName,
                settings.SearchItemCollectionName
            );
        }

        private static string GetSeedFilePath(string fileName)
        {
            return Path.Combine(AppContext.BaseDirectory, "Resources", "InitializingDatas", fileName);
        }

        private static async Task EnsureCollectionExistsAsync(IMongoDatabase database, string collectionName)
        {
            BsonDocument filter = new("name", collectionName);
            IAsyncCursor<BsonDocument>? collections =
                await database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            bool exists = await collections.AnyAsync();

            if (!exists)
            {
                await database.CreateCollectionAsync(collectionName);
            }
        }

        // ==================== INITIALISATION PARKS ====================

        private static async Task InitializeParksCollection(
            IMongoDatabase database,
            string collectionName,
            string jsonFilePath)
        {
            BsonDocument filter = new("name", collectionName);
            IAsyncCursor<BsonDocument>? collections =
                await database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            bool exists = await collections.AnyAsync();

            if (!exists)
            {
                await database.CreateCollectionAsync(collectionName);
            }

            IMongoCollection<Park>? parksCollection = database.GetCollection<Park>(collectionName);

            long documentCount = await parksCollection.EstimatedDocumentCountAsync();

            if (documentCount == 0)
            {
                if (!File.Exists(jsonFilePath))
                {
                    // À toi de voir : log, throw, etc.
                    Console.WriteLine($"[MongoDbInitializer] Fichier parks JSON introuvable : {jsonFilePath}");
                    return;
                }

                string json = await File.ReadAllTextAsync(jsonFilePath);

                JsonSerializerOptions options = new()
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    PropertyNameCaseInsensitive = true
                };

                List<ParkJson>? parksJson = JsonSerializer.Deserialize<List<ParkJson>>(json, options);

                // Index 2dsphere sur la collection Park
                await parksCollection.Indexes.CreateOneAsync(new CreateIndexModel<Park>(
                    Builders<Park>.IndexKeys.Geo2DSphere(park => park.Location)));

                List<Park> parks = new();

                if (parksJson is not null)
                {
                    foreach (ParkJson parkJson in parksJson)
                    {
                        Park park = new()
                        {
                            Name = parkJson.Name,
                            CountryCode = ExtractCountryCode(parkJson.Country.Name),
                            Latitude = parkJson.Latitude,
                            Longitude = parkJson.Longitude
                        };

                        parks.Add(park);
                    }
                }

                if (parks is { Count: > 0 })
                {
                    await parksCollection.InsertManyAsync(parks);
                }
            }
        }

        private static string ExtractCountryCode(string? countryName)
        {
            if (string.IsNullOrEmpty(countryName) ||
                !CountryToCode.TryGetValue(countryName, out string? code))
            {
                return "Unknown";
            }
            return code;
        }


        private static readonly Dictionary<string, string> CountryToCode = new()
        {
            // List of countries without prefix
            {"Albania", "AL"},
            {"Andorra", "AD"},
            {"Armenia", "AM"},
            {"Belarus", "BY"},
            {"Bosnia and Herzegovina", "BA"},
            {"Bulgaria", "BG"},
            {"Croatia", "HR"},
            {"Egypt", "EG"},
            {"Estonia", "EE"},
            {"Georgia", "GE"},
            {"Greece", "GR"},
            {"Haiti", "HT"},
            {"Iran", "IR"},
            {"Jamaica", "JM"},
            {"Kazakhstan", "KZ"},
            {"Kosovo", "XK"},
            {"Latvia", "LV"},
            {"Lithuania", "LT"},
            {"Luxembourg", "LU"},
            {"Malta", "MT"},
            {"Moldova", "MD"},
            {"Montenegro", "ME"},
            {"North Korea", "KP"},
            {"North Macedonia", "MK"},
            {"Oman", "OM"},
            {"Pakistan", "PK"},
            {"Palestine", "PS"},
            {"Philippines", "PH"},
            {"Romania", "RO"},
            {"Saudi Arabia", "SA"},
            {"Serbia", "RS"},
            {"Slovakia", "SK"},
            {"Slovenia", "SI"},
            {"Sri Lanka", "LK"},
            {"Tunisia", "TN"},
            {"Uzbekistan", "UZ"},

            // List of countries with "country." prefix
            {"country.argentina", "AR"},
            {"country.australia", "AU"},
            {"country.austria", "AT"},
            {"country.belgium", "BE"},
            {"country.brazil", "BR"},
            {"country.burma", "MM"},
            {"country.canada", "CA"},
            {"country.china", "CN"},
            {"country.colombia", "CO"},
            {"country.cyprus", "CY"},
            {"country.czech", "CZ"},
            {"country.denmark", "DK"},
            {"country.finland", "FI"},
            {"country.france", "FR"},
            {"country.germany", "DE"},
            {"country.guatemala", "GT"},
            {"country.hungary", "HU"},
            {"country.india", "IN"},
            {"country.indonesia", "ID"},
            {"country.iraq", "IQ"},
            {"country.ireland", "IE"},
            {"country.israel", "IL"},
            {"country.italy", "IT"},
            {"country.japan", "JP"},
            {"country.lebanon", "LB"},
            {"country.malaysia", "MY"},
            {"country.mexico", "MX"},
            {"country.mongolia", "MN"},
            {"country.na", "N/A"},  // Placeholder for "N/A", replace as needed
            {"country.netherlands", "NL"},
            {"country.newzealand", "NZ"},
            {"country.norway", "NO"},
            {"country.peru", "PE"},
            {"country.poland", "PL"},
            {"country.portugal", "PT"},
            {"country.qatar", "QA"},
            {"country.russia", "RU"},
            {"country.singapore", "SG"},
            {"country.southafrica", "ZA"},
            {"country.southkorea", "KR"},
            {"country.spain", "ES"},
            {"country.sweden", "SE"},
            {"country.switzerland", "CH"},
            {"country.taiwan", "TW"},
            {"country.thailand", "TH"},
            {"country.turkey", "TR"},
            {"country.uae", "AE"},
            {"country.uk", "GB"},
            {"country.ukraine", "UA"},
            {"country.usa", "US"},
            {"country.vietnam", "VN"}
        };

        private static async Task InitializeCountriesCollection(
                  IMongoDatabase database,
                  string collectionName,
                  string jsonFilePath)
        {
            // Vérifier/Créer la collection
            BsonDocument filter = new("name", collectionName);
            IAsyncCursor<BsonDocument>? collections =
                await database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            bool exists = await collections.AnyAsync();

            if (!exists)
            {
                await database.CreateCollectionAsync(collectionName);
            }

            IMongoCollection<Country>? countriesCollection =
                database.GetCollection<Country>(collectionName);

            long documentCount =
                await countriesCollection.EstimatedDocumentCountAsync();

            // On ne remplit que si la collection est vide
            if (documentCount > 0)
            {
                return;
            }

            if (!File.Exists(jsonFilePath))
            {
                Console.WriteLine($"[MongoDbInitializer] Fichier countries JSON introuvable : {jsonFilePath}");
                return;
            }

            string json = await File.ReadAllTextAsync(jsonFilePath);

            JsonSerializerOptions options = new()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNameCaseInsensitive = true
            };

            // 🔹 Si ton JSON est déjà au format Country (isoCode, names[ { languageCode, value } ])
            List<Country>? countries =
                JsonSerializer.Deserialize<List<Country>>(json, options);

            if (countries is { Count: > 0 })
            {
                await countriesCollection.InsertManyAsync(countries);
            }
            else
            {
                Console.WriteLine("[MongoDbInitializer] Aucun pays désérialisé depuis le JSON.");
            }
        }
        

        private static async Task InitializeParkItemsIndexesAsync(
            IMongoDatabase database,
            string collectionName)
        {
            IMongoCollection<ParkItem> itemsCollection = database.GetCollection<ParkItem>(collectionName);

            await itemsCollection.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<ParkItem>(Builders<ParkItem>.IndexKeys.Ascending(item => item.ParkId)),
                new CreateIndexModel<ParkItem>(Builders<ParkItem>.IndexKeys.Ascending(item => item.ZoneId)),
                new CreateIndexModel<ParkItem>(Builders<ParkItem>.IndexKeys.Ascending("attractionDetails.manufacturerId")),
                new CreateIndexModel<ParkItem>(Builders<ParkItem>.IndexKeys.Geo2DSphere(item => item.Location))
            });
        }

        private static async Task InitializeImageTagsIndexesAsync(
            IMongoDatabase database,
            string collectionName)
        {
            IMongoCollection<Entities.Model.Images.ImageTag> tagsCollection = database.GetCollection<Entities.Model.Images.ImageTag>(collectionName);

            await tagsCollection.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<Entities.Model.Images.ImageTag>(Builders<Entities.Model.Images.ImageTag>.IndexKeys.Ascending(x => x.Slug), new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<Entities.Model.Images.ImageTag>(Builders<Entities.Model.Images.ImageTag>.IndexKeys.Ascending(x => x.IsActive))
            });
        }

        private static async Task InitializeImagesIndexesAsync(
            IMongoDatabase database,
            string collectionName)
        {
            IMongoCollection<Entities.Model.Images.Image> imagesCollection =
                database.GetCollection<Entities.Model.Images.Image>(collectionName);

            await imagesCollection.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<Entities.Model.Images.Image>(
                    Builders<Entities.Model.Images.Image>.IndexKeys
                        .Ascending(img => img.OwnerType)
                        .Ascending(img => img.OwnerId)
                        .Ascending(img => img.Category)
                        .Ascending(img => img.IsCurrent)),

                new CreateIndexModel<Entities.Model.Images.Image>(
                    Builders<Entities.Model.Images.Image>.IndexKeys
                        .Ascending(img => img.Category)),

                new CreateIndexModel<Entities.Model.Images.Image>(
                    Builders<Entities.Model.Images.Image>.IndexKeys
                        .Ascending(img => img.CreatedAt))
            });
        }
    }
}