using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Common.Users;
using Entities.Model.Countries;
using Entities.Model.Images;
using Entities.Model.Parks;
using Entities.Model.Searching;
using Entities.Model.Users;
using MongoDB.Bson;
using MongoDB.Driver;
using Repositories.Interfaces;
using WebAPI.Features.CaptainCoaster.Models;
using WebAPI.Settings.Security;

namespace WebAPI.Settings.MongoDB
{
    public static class MongoDbInitializer
    {
        public static async Task InitializeCollectionsAsync(
            IMongoDatabase database,
            IMongoDbSettings settings,
            AdminSeedSettings adminSeedSettings)
        {
            await EnsureCollectionExistsAsync(database, settings.UsersCollectionName);
            await InitializeUsersIndexesAsync(database, settings.UsersCollectionName);

            await EnsureCollectionExistsAsync(database, settings.ImagesCollectionName);
            await InitializeImagesIndexesAsync(database, settings.ImagesCollectionName);

            await EnsureCollectionExistsAsync(database, settings.ImageTagsCollectionName);
            await InitializeImageTagsIndexesAsync(database, settings.ImageTagsCollectionName);

            await EnsureCollectionExistsAsync(database, settings.CountriesCollectionName);
            await InitializeCountriesIndexesAsync(database, settings.CountriesCollectionName);
            await InitializeCountriesCollection(
                database,
                settings.CountriesCollectionName,
                GetSeedFilePath("countries.seed.json"));

            await EnsureCollectionExistsAsync(database, settings.ParksCollectionName);
            await InitializeParksIndexesAsync(database, settings.ParksCollectionName);

            await EnsureCollectionExistsAsync(database, settings.ParkFoundersCollectionName);
            await InitializeParkFoundersIndexesAsync(database, settings.ParkFoundersCollectionName);

            await EnsureCollectionExistsAsync(database, settings.ParkOperatorsCollectionName);
            await InitializeParkOperatorsIndexesAsync(database, settings.ParkOperatorsCollectionName);

            await EnsureCollectionExistsAsync(database, settings.AttractionManufacturersCollectionName);
            await InitializeAttractionManufacturersIndexesAsync(database, settings.AttractionManufacturersCollectionName);

            await EnsureCollectionExistsAsync(database, settings.ParkZonesCollectionName);
            await InitializeParkZonesIndexesAsync(database, settings.ParkZonesCollectionName);

            await EnsureCollectionExistsAsync(database, settings.ParkItemsCollectionName);
            await InitializeParkItemsIndexesAsync(database, settings.ParkItemsCollectionName);

            await EnsureCollectionExistsAsync(database, settings.SearchItemCollectionName);
            await InitializeSearchItemsIndexesAsync(database, settings.SearchItemCollectionName);

            await EnsureCollectionExistsAsync(database, settings.CaptainCoasterSettingsCollectionName);
            await InitializeCaptainCoasterSettingsIndexesAsync(database, settings.CaptainCoasterSettingsCollectionName);

            await EnsureCollectionExistsAsync(database, settings.CaptainCoasterParksCollectionName);
            await InitializeCaptainCoasterParksIndexesAsync(database, settings.CaptainCoasterParksCollectionName);

            await EnsureCollectionExistsAsync(database, settings.CaptainCoasterCoastersCollectionName);
            await InitializeCaptainCoasterCoastersIndexesAsync(database, settings.CaptainCoasterCoastersCollectionName);

            await EnsureCollectionExistsAsync(database, settings.CaptainCoasterSyncSessionsCollectionName);
            await InitializeCaptainCoasterSyncSessionsIndexesAsync(database, settings.CaptainCoasterSyncSessionsCollectionName);

            await EnsureCollectionExistsAsync(database, settings.CaptainCoasterComparisonResultsCollectionName);
            await InitializeCaptainCoasterComparisonResultsIndexesAsync(database, settings.CaptainCoasterComparisonResultsCollectionName);

            await InitializeAdminUserAsync(database, settings.UsersCollectionName, adminSeedSettings);
        }

        private static string GetSeedFilePath(string fileName)
        {
            return Path.Combine(AppContext.BaseDirectory, "Resources", "InitializingDatas", fileName);
        }

        private static async Task EnsureCollectionExistsAsync(IMongoDatabase database, string collectionName)
        {
            BsonDocument filter = new("name", collectionName);
            IAsyncCursor<BsonDocument> collections =
                await database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            bool exists = await collections.AnyAsync();

            if (!exists)
            {
                await database.CreateCollectionAsync(collectionName);
            }
        }

        private static async Task InitializeAdminUserAsync(
            IMongoDatabase database,
            string collectionName,
            AdminSeedSettings settings)
        {
            if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.Email))
            {
                return;
            }

            IMongoCollection<User> usersCollection = database.GetCollection<User>(collectionName);
            string normalizedEmail = settings.Email.Trim().ToLowerInvariant();

            User? existingUser = await usersCollection
                .Find(user => user.Email == normalizedEmail)
                .FirstOrDefaultAsync();

            if (existingUser is null)
            {
                if (string.IsNullOrWhiteSpace(settings.Password))
                {
                    Console.WriteLine("[MongoDbInitializer] Admin seed skipped because Initialization:AdminUser:Password is empty.");
                    return;
                }

                DateTime now = DateTime.UtcNow;
                User adminUser = new()
                {
                    Email = normalizedEmail,
                    FirstName = string.IsNullOrWhiteSpace(settings.FirstName) ? "Ced" : settings.FirstName.Trim(),
                    LastName = string.IsNullOrWhiteSpace(settings.LastName) ? "Caudron" : settings.LastName.Trim(),
                    PreferredLanguage = string.IsNullOrWhiteSpace(settings.PreferredLanguage)
                        ? "FR"
                        : settings.PreferredLanguage.Trim().ToUpperInvariant(),
                    HashedPassword = BCrypt.Net.BCrypt.HashPassword(settings.Password),
                    IsActivated = true,
                    IsBlocked = false,
                    Roles = new List<Role>
                    {
                        Role.USER,
                        Role.MODERATOR,
                        Role.ADMIN
                    },
                    CreatedAt = now,
                    UpdatedAt = now,
                    LastLogin = now,
                    LastActivity = now
                };

                await usersCollection.InsertOneAsync(adminUser);
                return;
            }

            bool needsUpdate = false;

            if (!existingUser.IsActivated)
            {
                existingUser.IsActivated = true;
                needsUpdate = true;
            }

            if (existingUser.IsBlocked)
            {
                existingUser.IsBlocked = false;
                needsUpdate = true;
            }

            foreach (Role role in new[] { Role.USER, Role.MODERATOR, Role.ADMIN })
            {
                if (!existingUser.Roles.Contains(role))
                {
                    existingUser.Roles.Add(role);
                    needsUpdate = true;
                }
            }

            if (string.IsNullOrWhiteSpace(existingUser.HashedPassword) && !string.IsNullOrWhiteSpace(settings.Password))
            {
                existingUser.HashedPassword = BCrypt.Net.BCrypt.HashPassword(settings.Password);
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                existingUser.UpdatedAt = DateTime.UtcNow;
                await usersCollection.ReplaceOneAsync(user => user.Id == existingUser.Id, existingUser);
            }
        }

        private static async Task InitializeCountriesCollection(
            IMongoDatabase database,
            string collectionName,
            string jsonFilePath)
        {
            IMongoCollection<Country> countriesCollection = database.GetCollection<Country>(collectionName);
            long documentCount = await countriesCollection.EstimatedDocumentCountAsync();

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

            List<Country>? countries = JsonSerializer.Deserialize<List<Country>>(json, options);

            if (countries is { Count: > 0 })
            {
                await countriesCollection.InsertManyAsync(countries);
            }
            else
            {
                Console.WriteLine("[MongoDbInitializer] Aucun pays désérialisé depuis le JSON.");
            }
        }

        private static async Task InitializeUsersIndexesAsync(IMongoDatabase database, string collectionName)
        {
            IMongoCollection<User> usersCollection = database.GetCollection<User>(collectionName);

            List<CreateIndexModel<User>> indexes = new()
            {
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(user => user.Email),
                    new CreateIndexOptions<User>
                    {
                        Name = "idx_users_email_unique",
                        Unique = true,
                        PartialFilterExpression = Builders<User>.Filter.Type(user => user.Email, BsonType.String)
                    }),
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending("externalLogins.provider").Ascending("externalLogins.providerUserId"),
                    new CreateIndexOptions { Name = "idx_users_external_login" }),
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(user => user.EmailConfirmationTokenHash),
                    new CreateIndexOptions<User>
                    {
                        Name = "idx_users_email_confirmation_token",
                        PartialFilterExpression = Builders<User>.Filter.Type(user => user.EmailConfirmationTokenHash, BsonType.String)
                    }),
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(user => user.PasswordResetTokenHash),
                    new CreateIndexOptions<User>
                    {
                        Name = "idx_users_password_reset_token",
                        PartialFilterExpression = Builders<User>.Filter.Type(user => user.PasswordResetTokenHash, BsonType.String)
                    }),
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(user => user.IsActivated).Ascending(user => user.IsBlocked),
                    new CreateIndexOptions { Name = "idx_users_activation_blocking" })
            };

            await usersCollection.Indexes.CreateManyAsync(indexes);
        }

        private static async Task InitializeCountriesIndexesAsync(IMongoDatabase database, string collectionName)
        {
            IMongoCollection<Country> countriesCollection = database.GetCollection<Country>(collectionName);

            List<CreateIndexModel<Country>> indexes = new()
            {
                new CreateIndexModel<Country>(
                    Builders<Country>.IndexKeys.Ascending(country => country.IsoCode),
                    new CreateIndexOptions { Name = "idx_countries_iso_code_unique", Unique = true })
            };

            await countriesCollection.Indexes.CreateManyAsync(indexes);
        }

        private static async Task InitializeParksIndexesAsync(IMongoDatabase database, string collectionName)
        {
            IMongoCollection<Park> parksCollection = database.GetCollection<Park>(collectionName);

            List<CreateIndexModel<Park>> indexes = new()
            {
                new CreateIndexModel<Park>(Builders<Park>.IndexKeys.Geo2DSphere(park => park.Location)),
                new CreateIndexModel<Park>(Builders<Park>.IndexKeys.Ascending(park => park.IsVisible), new CreateIndexOptions { Name = "idx_parks_is_visible" }),
                new CreateIndexModel<Park>(Builders<Park>.IndexKeys.Ascending(park => park.CountryCode), new CreateIndexOptions { Name = "idx_parks_country_code" }),
                new CreateIndexModel<Park>(Builders<Park>.IndexKeys.Ascending(park => park.FounderId), new CreateIndexOptions { Name = "idx_parks_founder_id" }),
                new CreateIndexModel<Park>(Builders<Park>.IndexKeys.Ascending(park => park.OperatorId), new CreateIndexOptions { Name = "idx_parks_operator_id" }),
                new CreateIndexModel<Park>(Builders<Park>.IndexKeys.Ascending(park => park.CurrentLogoImageId), new CreateIndexOptions { Name = "idx_parks_current_logo_image_id" })
            };

            await parksCollection.Indexes.CreateManyAsync(indexes);
        }

        private static async Task InitializeParkFoundersIndexesAsync(IMongoDatabase database, string collectionName)
        {
            IMongoCollection<ParkFounder> foundersCollection = database.GetCollection<ParkFounder>(collectionName);

            await foundersCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<ParkFounder>(
                    Builders<ParkFounder>.IndexKeys.Ascending(founder => founder.Name),
                    new CreateIndexOptions { Name = "idx_park_founders_name" }));
        }

        private static async Task InitializeParkOperatorsIndexesAsync(IMongoDatabase database, string collectionName)
        {
            IMongoCollection<ParkOperator> operatorsCollection = database.GetCollection<ParkOperator>(collectionName);

            await operatorsCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<ParkOperator>(
                    Builders<ParkOperator>.IndexKeys.Ascending(item => item.Name),
                    new CreateIndexOptions { Name = "idx_park_operators_name" }));
        }

        private static async Task InitializeAttractionManufacturersIndexesAsync(IMongoDatabase database, string collectionName)
        {
            IMongoCollection<AttractionManufacturer> manufacturersCollection = database.GetCollection<AttractionManufacturer>(collectionName);

            await manufacturersCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<AttractionManufacturer>(
                    Builders<AttractionManufacturer>.IndexKeys.Ascending(item => item.Name),
                    new CreateIndexOptions { Name = "idx_attraction_manufacturers_name" }));
        }

        private static async Task InitializeParkZonesIndexesAsync(IMongoDatabase database, string collectionName)
        {
            IMongoCollection<ParkZone> zonesCollection = database.GetCollection<ParkZone>(collectionName);

            List<CreateIndexModel<ParkZone>> indexes = new()
            {
                new CreateIndexModel<ParkZone>(
                    Builders<ParkZone>.IndexKeys.Ascending(zone => zone.ParkId).Ascending(zone => zone.SortOrder).Ascending(zone => zone.Name),
                    new CreateIndexOptions { Name = "idx_park_zones_park_sort_name" }),
                new CreateIndexModel<ParkZone>(
                    Builders<ParkZone>.IndexKeys.Ascending(zone => zone.ParkId).Ascending(zone => zone.IsVisible),
                    new CreateIndexOptions { Name = "idx_park_zones_park_visibility" }),
                new CreateIndexModel<ParkZone>(
                    Builders<ParkZone>.IndexKeys.Ascending(zone => zone.Slug),
                    new CreateIndexOptions<ParkZone>
                    {
                        Name = "idx_park_zones_slug",
                        PartialFilterExpression = Builders<ParkZone>.Filter.Type(zone => zone.Slug, BsonType.String)
                    })
            };

            await zonesCollection.Indexes.CreateManyAsync(indexes);
        }

        private static async Task InitializeParkItemsIndexesAsync(IMongoDatabase database, string collectionName)
        {
            IMongoCollection<ParkItem> itemsCollection = database.GetCollection<ParkItem>(collectionName);

            List<CreateIndexModel<ParkItem>> indexes = new()
            {
                new CreateIndexModel<ParkItem>(
                    Builders<ParkItem>.IndexKeys.Ascending(item => item.ParkId).Ascending(item => item.IsVisible).Ascending(item => item.Name),
                    new CreateIndexOptions { Name = "idx_park_items_park_visibility_name" }),
                new CreateIndexModel<ParkItem>(
                    Builders<ParkItem>.IndexKeys.Ascending(item => item.ParkId).Ascending(item => item.Category).Ascending(item => item.Type).Ascending(item => item.Name),
                    new CreateIndexOptions { Name = "idx_park_items_park_category_type_name" }),
                new CreateIndexModel<ParkItem>(
                    Builders<ParkItem>.IndexKeys.Ascending(item => item.ZoneId)),
                new CreateIndexModel<ParkItem>(
                    Builders<ParkItem>.IndexKeys.Ascending("attractionDetails.manufacturerId")),
                new CreateIndexModel<ParkItem>(
                    Builders<ParkItem>.IndexKeys.Ascending(item => item.Category).Ascending("attractionDetails.manufacturerId"),
                    new CreateIndexOptions { Name = "idx_park_items_category_manufacturer_id" }),
                new CreateIndexModel<ParkItem>(
                    Builders<ParkItem>.IndexKeys.Geo2DSphere(item => item.Location))
            };

            await itemsCollection.Indexes.CreateManyAsync(indexes);
        }

        private static async Task InitializeSearchItemsIndexesAsync(IMongoDatabase database, string collectionName)
        {
            IMongoCollection<SearchItem> searchCollection = database.GetCollection<SearchItem>(collectionName);

            IndexKeysDefinition<SearchItem> textIndexKeys = Builders<SearchItem>.IndexKeys
                .Text(item => item.Title)
                .Text(item => item.Description)
                .Text(item => item.Keywords);

            List<CreateIndexModel<SearchItem>> indexes = new()
            {
                new CreateIndexModel<SearchItem>(
                    Builders<SearchItem>.IndexKeys.Ascending(item => item.OriginalId),
                    new CreateIndexOptions { Name = "idx_search_items_original_id_unique", Unique = true }),
                new CreateIndexModel<SearchItem>(
                    Builders<SearchItem>.IndexKeys.Geo2DSphere(item => item.Location)),
                new CreateIndexModel<SearchItem>(
                    Builders<SearchItem>.IndexKeys.Ascending(item => item.Category).Ascending(item => item.IsVisible).Descending(item => item.UpdatedAt),
                    new CreateIndexOptions { Name = "idx_search_items_category_visibility_updated" }),
                new CreateIndexModel<SearchItem>(
                    textIndexKeys,
                    new CreateIndexOptions { DefaultLanguage = "french", Name = "Idx_SearchItem_Text" })
            };

            await searchCollection.Indexes.CreateManyAsync(indexes);
        }

        private static async Task InitializeImagesIndexesAsync(IMongoDatabase database, string collectionName)
        {
            IMongoCollection<Image> imagesCollection = database.GetCollection<Image>(collectionName);

            List<CreateIndexModel<Image>> indexes = new()
            {
                new CreateIndexModel<Image>(
                    Builders<Image>.IndexKeys
                        .Ascending(img => img.OwnerType)
                        .Ascending(img => img.OwnerId)
                        .Ascending(img => img.Category)
                        .Ascending(img => img.IsCurrent)),
                new CreateIndexModel<Image>(
                    Builders<Image>.IndexKeys.Ascending(img => img.OwnerType).Ascending(img => img.OwnerId),
                    new CreateIndexOptions { Name = "idx_images_owner" }),
                new CreateIndexModel<Image>(
                    Builders<Image>.IndexKeys.Ascending(img => img.Category)),
                new CreateIndexModel<Image>(
                    Builders<Image>.IndexKeys.Ascending(img => img.CreatedAt)),
                new CreateIndexModel<Image>(
                    Builders<Image>.IndexKeys.Ascending("tagIds"),
                    new CreateIndexOptions { Name = "idx_images_tag_ids" })
            };

            await imagesCollection.Indexes.CreateManyAsync(indexes);
        }

        private static async Task InitializeImageTagsIndexesAsync(IMongoDatabase database, string collectionName)
        {
            IMongoCollection<ImageTag> tagsCollection = database.GetCollection<ImageTag>(collectionName);

            List<CreateIndexModel<ImageTag>> indexes = new()
            {
                new CreateIndexModel<ImageTag>(
                    Builders<ImageTag>.IndexKeys.Ascending(item => item.Slug),
                    new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<ImageTag>(
                    Builders<ImageTag>.IndexKeys.Ascending(item => item.IsActive))
            };

            await tagsCollection.Indexes.CreateManyAsync(indexes);
        }

        private static async Task DropIndexIfExistsAsync<TDocument>(IMongoCollection<TDocument> collection, string indexName)
        {
            using IAsyncCursor<BsonDocument> cursor = await collection.Indexes.ListAsync();
            List<BsonDocument> indexes = await cursor.ToListAsync();
            bool exists = indexes.Any(item => item.TryGetValue("name", out BsonValue value) && value.AsString == indexName);
            if (exists)
            {
                await collection.Indexes.DropOneAsync(indexName);
            }
        }

        private static async Task InitializeCaptainCoasterSettingsIndexesAsync(IMongoDatabase database, string collectionName)
        {
            IMongoCollection<CaptainCoasterDataSourceSettings> collection = database.GetCollection<CaptainCoasterDataSourceSettings>(collectionName);

            await collection.Indexes.CreateOneAsync(
                new CreateIndexModel<CaptainCoasterDataSourceSettings>(
                    Builders<CaptainCoasterDataSourceSettings>.IndexKeys.Ascending(item => item.Source),
                    new CreateIndexOptions { Name = "idx_cc_settings_source_unique", Unique = true }));
        }

        private static async Task InitializeCaptainCoasterParksIndexesAsync(IMongoDatabase database, string collectionName)
        {
            IMongoCollection<CaptainCoasterParkSnapshot> collection = database.GetCollection<CaptainCoasterParkSnapshot>(collectionName);
            await DropIndexIfExistsAsync(collection, "idx_cc_parks_session_external_id_unique");

            List<CreateIndexModel<CaptainCoasterParkSnapshot>> indexes = new()
            {
                new CreateIndexModel<CaptainCoasterParkSnapshot>(
                    Builders<CaptainCoasterParkSnapshot>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.CaptainCoasterId),
                    new CreateIndexOptions { Name = "idx_cc_parks_session_external_id" }),
                new CreateIndexModel<CaptainCoasterParkSnapshot>(
                    Builders<CaptainCoasterParkSnapshot>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.Name),
                    new CreateIndexOptions { Name = "idx_cc_parks_session_name" }),
                new CreateIndexModel<CaptainCoasterParkSnapshot>(
                    Builders<CaptainCoasterParkSnapshot>.IndexKeys.Ascending(item => item.ScrapedAtUtc),
                    new CreateIndexOptions { Name = "idx_cc_parks_scraped_at" })
            };

            await collection.Indexes.CreateManyAsync(indexes);
        }

        private static async Task InitializeCaptainCoasterCoastersIndexesAsync(IMongoDatabase database, string collectionName)
        {
            IMongoCollection<CaptainCoasterCoasterSnapshot> collection = database.GetCollection<CaptainCoasterCoasterSnapshot>(collectionName);
            await DropIndexIfExistsAsync(collection, "idx_cc_coasters_session_external_id_unique");

            List<CreateIndexModel<CaptainCoasterCoasterSnapshot>> indexes = new()
            {
                new CreateIndexModel<CaptainCoasterCoasterSnapshot>(
                    Builders<CaptainCoasterCoasterSnapshot>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.CaptainCoasterId),
                    new CreateIndexOptions { Name = "idx_cc_coasters_session_external_id" }),
                new CreateIndexModel<CaptainCoasterCoasterSnapshot>(
                    Builders<CaptainCoasterCoasterSnapshot>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.ParkCaptainCoasterId),
                    new CreateIndexOptions { Name = "idx_cc_coasters_session_park_external_id" }),
                new CreateIndexModel<CaptainCoasterCoasterSnapshot>(
                    Builders<CaptainCoasterCoasterSnapshot>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.ParkName),
                    new CreateIndexOptions { Name = "idx_cc_coasters_session_park_name" }),
                new CreateIndexModel<CaptainCoasterCoasterSnapshot>(
                    Builders<CaptainCoasterCoasterSnapshot>.IndexKeys.Ascending(item => item.Manufacturer),
                    new CreateIndexOptions { Name = "idx_cc_coasters_manufacturer" })
            };

            await collection.Indexes.CreateManyAsync(indexes);
        }

        private static async Task InitializeCaptainCoasterSyncSessionsIndexesAsync(IMongoDatabase database, string collectionName)
        {
            IMongoCollection<CaptainCoasterSyncSession> collection = database.GetCollection<CaptainCoasterSyncSession>(collectionName);

            List<CreateIndexModel<CaptainCoasterSyncSession>> indexes = new()
            {
                new CreateIndexModel<CaptainCoasterSyncSession>(
                    Builders<CaptainCoasterSyncSession>.IndexKeys.Descending(item => item.StartedAtUtc),
                    new CreateIndexOptions { Name = "idx_cc_sessions_started_at_desc" }),
                new CreateIndexModel<CaptainCoasterSyncSession>(
                    Builders<CaptainCoasterSyncSession>.IndexKeys.Ascending(item => item.Status).Descending(item => item.StartedAtUtc),
                    new CreateIndexOptions { Name = "idx_cc_sessions_status_started_at" })
            };

            await collection.Indexes.CreateManyAsync(indexes);
        }

        private static async Task InitializeCaptainCoasterComparisonResultsIndexesAsync(IMongoDatabase database, string collectionName)
        {
            IMongoCollection<CaptainCoasterComparisonResult> collection = database.GetCollection<CaptainCoasterComparisonResult>(collectionName);

            List<CreateIndexModel<CaptainCoasterComparisonResult>> indexes = new()
            {
                new CreateIndexModel<CaptainCoasterComparisonResult>(
                    Builders<CaptainCoasterComparisonResult>.IndexKeys
                        .Ascending(item => item.SyncSessionId)
                        .Ascending(item => item.EntityType)
                        .Ascending(item => item.ChangeType)
                        .Ascending(item => item.DisplayName),
                    new CreateIndexOptions { Name = "idx_cc_comparisons_session_entity_change_display" }),
                new CreateIndexModel<CaptainCoasterComparisonResult>(
                    Builders<CaptainCoasterComparisonResult>.IndexKeys
                        .Ascending(item => item.SyncSessionId)
                        .Ascending(item => item.IsApplied),
                    new CreateIndexOptions { Name = "idx_cc_comparisons_session_is_applied" }),
                new CreateIndexModel<CaptainCoasterComparisonResult>(
                    Builders<CaptainCoasterComparisonResult>.IndexKeys
                        .Ascending(item => item.SyncSessionId)
                        .Ascending(item => item.ChangeType),
                    new CreateIndexOptions { Name = "idx_cc_comparisons_session_change_type" }),
                new CreateIndexModel<CaptainCoasterComparisonResult>(
                    Builders<CaptainCoasterComparisonResult>.IndexKeys
                        .Ascending(item => item.SyncSessionId)
                        .Ascending(item => item.ExternalEntityId),
                    new CreateIndexOptions<CaptainCoasterComparisonResult>
                    {
                        Name = "idx_cc_comparisons_session_external_entity_id",
                        PartialFilterExpression = Builders<CaptainCoasterComparisonResult>.Filter.Type(item => item.ExternalEntityId, BsonType.String)
                    })
            };

            await collection.Indexes.CreateManyAsync(indexes);
        }
    }
}
