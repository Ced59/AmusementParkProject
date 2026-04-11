using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Configuration.Initialization;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Countries;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

/// <summary>
/// Initialise les collections MongoDB métier, leurs index et les seeds de base.
/// </summary>
public sealed class MongoDatabaseInitializer
{
    private readonly IMongoDatabase database;
    private readonly MongoDbSettings settings;
    private readonly AdminSeedSettings adminSeedSettings;
    private readonly IHostEnvironment hostEnvironment;

    public MongoDatabaseInitializer(
        IMongoDatabase database,
        MongoDbSettings settings,
        AdminSeedSettings adminSeedSettings,
        IHostEnvironment hostEnvironment)
    {
        this.database = database;
        this.settings = settings;
        this.adminSeedSettings = adminSeedSettings;
        this.hostEnvironment = hostEnvironment;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await this.EnsureCollectionExistsAsync(this.settings.UsersCollectionName, cancellationToken);
        await this.InitializeUsersIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ImagesCollectionName, cancellationToken);
        await this.InitializeImagesIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ImageTagsCollectionName, cancellationToken);
        await this.InitializeImageTagsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.CountriesCollectionName, cancellationToken);
        await this.InitializeCountriesIndexesAsync(cancellationToken);
        await this.SeedCountriesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParksCollectionName, cancellationToken);
        await this.InitializeParksIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParkFoundersCollectionName, cancellationToken);
        await this.InitializeParkFoundersIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParkOperatorsCollectionName, cancellationToken);
        await this.InitializeParkOperatorsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.AttractionManufacturersCollectionName, cancellationToken);
        await this.InitializeAttractionManufacturersIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParkZonesCollectionName, cancellationToken);
        await this.InitializeParkZonesIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParkItemsCollectionName, cancellationToken);
        await this.InitializeParkItemsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.SearchItemCollectionName, cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.CaptainCoasterSettingsCollectionName, cancellationToken);
        await this.InitializeCaptainCoasterSettingsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.CaptainCoasterParksCollectionName, cancellationToken);
        await this.InitializeCaptainCoasterParksIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.CaptainCoasterCoastersCollectionName, cancellationToken);
        await this.InitializeCaptainCoasterCoastersIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.CaptainCoasterDiscoveredUrlsCollectionName, cancellationToken);
        await this.InitializeCaptainCoasterDiscoveredUrlsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.CaptainCoasterSyncSessionsCollectionName, cancellationToken);
        await this.InitializeCaptainCoasterSyncSessionsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.CaptainCoasterComparisonResultsCollectionName, cancellationToken);
        await this.InitializeCaptainCoasterComparisonResultsIndexesAsync(cancellationToken);

        await this.InitializeAdminUserAsync(cancellationToken);
    }

    private async Task EnsureCollectionExistsAsync(string collectionName, CancellationToken cancellationToken)
    {
        BsonDocument filter = new BsonDocument("name", collectionName);
        ListCollectionsOptions options = new ListCollectionsOptions
        {
            Filter = filter,
        };

        using IAsyncCursor<BsonDocument> collections = await this.database.ListCollectionsAsync(options, cancellationToken);
        bool exists = await collections.AnyAsync(cancellationToken);

        if (!exists)
        {
            await this.database.CreateCollectionAsync(collectionName, cancellationToken: cancellationToken);
        }
    }

    private async Task InitializeAdminUserAsync(CancellationToken cancellationToken)
    {
        if (!this.adminSeedSettings.Enabled || string.IsNullOrWhiteSpace(this.adminSeedSettings.Email))
        {
            return;
        }

        IMongoCollection<UserDocument> usersCollection = this.database.GetCollection<UserDocument>(this.settings.UsersCollectionName);
        string normalizedEmail = this.adminSeedSettings.Email.Trim().ToLowerInvariant();

        UserDocument? existingUser = await usersCollection
            .Find(user => user.Email == normalizedEmail)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingUser is null)
        {
            if (string.IsNullOrWhiteSpace(this.adminSeedSettings.Password))
            {
                Console.WriteLine("[MongoDatabaseInitializer] Admin seed ignoré car Initialization:AdminUser:Password est vide.");
                return;
            }

            DateTime now = DateTime.UtcNow;
            UserDocument adminUser = new UserDocument
            {
                Email = normalizedEmail,
                FirstName = string.IsNullOrWhiteSpace(this.adminSeedSettings.FirstName) ? "Ced" : this.adminSeedSettings.FirstName.Trim(),
                LastName = string.IsNullOrWhiteSpace(this.adminSeedSettings.LastName) ? "Caudron" : this.adminSeedSettings.LastName.Trim(),
                PreferredLanguage = string.IsNullOrWhiteSpace(this.adminSeedSettings.PreferredLanguage)
                    ? "FR"
                    : this.adminSeedSettings.PreferredLanguage.Trim().ToUpperInvariant(),
                HashedPassword = BCrypt.Net.BCrypt.HashPassword(this.adminSeedSettings.Password),
                IsActivated = true,
                IsBlocked = false,
                Roles = new List<Role>
                {
                    Role.User,
                    Role.Moderator,
                    Role.Admin,
                },
                CreatedAt = now,
                UpdatedAt = now,
                LastLoginUtc = now,
                LastActivityUtc = now,
            };

            await usersCollection.InsertOneAsync(adminUser, cancellationToken: cancellationToken);
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

        foreach (Role role in new[] { Role.User, Role.Moderator, Role.Admin })
        {
            if (!existingUser.Roles.Contains(role))
            {
                existingUser.Roles.Add(role);
                needsUpdate = true;
            }
        }

        if (string.IsNullOrWhiteSpace(existingUser.HashedPassword) && !string.IsNullOrWhiteSpace(this.adminSeedSettings.Password))
        {
            existingUser.HashedPassword = BCrypt.Net.BCrypt.HashPassword(this.adminSeedSettings.Password);
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            existingUser.UpdatedAt = DateTime.UtcNow;
            await usersCollection.ReplaceOneAsync(user => user.Id == existingUser.Id, existingUser, cancellationToken: cancellationToken);
        }
    }

    private async Task SeedCountriesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CountryDocument> countriesCollection = this.database.GetCollection<CountryDocument>(this.settings.CountriesCollectionName);
        long documentCount = await countriesCollection.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);

        if (documentCount > 0)
        {
            return;
        }

        string jsonFilePath = this.ResolveCountriesSeedPath();
        if (!File.Exists(jsonFilePath))
        {
            Console.WriteLine($"[MongoDatabaseInitializer] Fichier countries JSON introuvable : {jsonFilePath}");
            return;
        }

        string json = await File.ReadAllTextAsync(jsonFilePath, cancellationToken);

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
        };

        List<CountrySeedItem>? seedItems = JsonSerializer.Deserialize<List<CountrySeedItem>>(json, options);
        if (seedItems is null || seedItems.Count == 0)
        {
            Console.WriteLine("[MongoDatabaseInitializer] Aucun pays désérialisé depuis le JSON.");
            return;
        }

        List<CountryDocument> countries = seedItems.Select(static item => new CountryDocument
        {
            Id = string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString("D") : item.Id,
            IsoCode = item.IsoCode?.Trim().ToUpperInvariant() ?? string.Empty,
            Names = item.Names.Select(static name => new LocalizedTextDocument
            {
                LanguageCode = name.LanguageCode?.Trim().ToLowerInvariant() ?? string.Empty,
                Value = name.Value,
            }).ToList(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        }).ToList();

        if (countries.Count > 0)
        {
            await countriesCollection.InsertManyAsync(countries, cancellationToken: cancellationToken);
        }
    }

    private string ResolveCountriesSeedPath()
    {
        string contentRootPath = this.hostEnvironment.ContentRootPath;
        string primaryPath = Path.Combine(contentRootPath, "Resources", "InitializingDatas", "countries.seed.json");
        if (File.Exists(primaryPath))
        {
            return primaryPath;
        }

        string fallbackPath = Path.Combine(AppContext.BaseDirectory, "Resources", "InitializingDatas", "countries.seed.json");
        return fallbackPath;
    }

    private async Task InitializeUsersIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<UserDocument> usersCollection = this.database.GetCollection<UserDocument>(this.settings.UsersCollectionName);
        List<CreateIndexModel<UserDocument>> indexes = new List<CreateIndexModel<UserDocument>>
        {
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(item => item.Email),
                new CreateIndexOptions { Name = "idx_users_email_unique", Unique = true }),
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Descending(item => item.LastActivityUtc),
                new CreateIndexOptions { Name = "idx_users_last_activity_desc" }),
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys
                    .Ascending("externalLogins.provider")
                    .Ascending("externalLogins.providerUserId"),
                new CreateIndexOptions { Name = "idx_users_external_login" }),
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(item => item.EmailConfirmationTokenHash),
                new CreateIndexOptions<UserDocument>
                {
                    Name = "idx_users_email_confirmation_token_hash",
                    PartialFilterExpression = Builders<UserDocument>.Filter.Type(item => item.EmailConfirmationTokenHash, BsonType.String),
                }),
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(item => item.PasswordResetTokenHash),
                new CreateIndexOptions<UserDocument>
                {
                    Name = "idx_users_password_reset_token_hash",
                    PartialFilterExpression = Builders<UserDocument>.Filter.Type(item => item.PasswordResetTokenHash, BsonType.String),
                }),
        };

        await usersCollection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeCountriesIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CountryDocument> countriesCollection = this.database.GetCollection<CountryDocument>(this.settings.CountriesCollectionName);
        List<CreateIndexModel<CountryDocument>> indexes = new List<CreateIndexModel<CountryDocument>>
        {
            new CreateIndexModel<CountryDocument>(
                Builders<CountryDocument>.IndexKeys.Ascending(item => item.IsoCode),
                new CreateIndexOptions { Name = "idx_countries_iso_code_unique", Unique = true }),
        };

        await countriesCollection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeParksIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkDocument> parksCollection = this.database.GetCollection<ParkDocument>(this.settings.ParksCollectionName);
        List<CreateIndexModel<ParkDocument>> indexes = new List<CreateIndexModel<ParkDocument>>
        {
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys.Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_parks_name" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys.Ascending(item => item.CountryCode),
                new CreateIndexOptions { Name = "idx_parks_country_code" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys.Ascending(item => item.IsVisible).Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_parks_visibility_updated" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys.Ascending(item => item.IsVisible).Ascending(item => item.Name).Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_parks_visibility_name_id" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys.Geo2DSphere(item => item.Location),
                new CreateIndexOptions { Name = "idx_parks_location" }),
        };

        await parksCollection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeParkFoundersIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkFounderDocument> collection = this.database.GetCollection<ParkFounderDocument>(this.settings.ParkFoundersCollectionName);
        List<CreateIndexModel<ParkFounderDocument>> indexes = new List<CreateIndexModel<ParkFounderDocument>>
        {
            new CreateIndexModel<ParkFounderDocument>(
                Builders<ParkFounderDocument>.IndexKeys.Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_park_founders_name_unique", Unique = true }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeParkOperatorsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkOperatorDocument> collection = this.database.GetCollection<ParkOperatorDocument>(this.settings.ParkOperatorsCollectionName);
        List<CreateIndexModel<ParkOperatorDocument>> indexes = new List<CreateIndexModel<ParkOperatorDocument>>
        {
            new CreateIndexModel<ParkOperatorDocument>(
                Builders<ParkOperatorDocument>.IndexKeys.Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_park_operators_name_unique", Unique = true }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeAttractionManufacturersIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<AttractionManufacturerDocument> collection = this.database.GetCollection<AttractionManufacturerDocument>(this.settings.AttractionManufacturersCollectionName);
        List<CreateIndexModel<AttractionManufacturerDocument>> indexes = new List<CreateIndexModel<AttractionManufacturerDocument>>
        {
            new CreateIndexModel<AttractionManufacturerDocument>(
                Builders<AttractionManufacturerDocument>.IndexKeys.Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_attraction_manufacturers_name_unique", Unique = true }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeParkZonesIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkZoneDocument> collection = this.database.GetCollection<ParkZoneDocument>(this.settings.ParkZonesCollectionName);
        List<CreateIndexModel<ParkZoneDocument>> indexes = new List<CreateIndexModel<ParkZoneDocument>>
        {
            new CreateIndexModel<ParkZoneDocument>(
                Builders<ParkZoneDocument>.IndexKeys.Ascending(item => item.ParkId).Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_park_zones_park_name" }),
            new CreateIndexModel<ParkZoneDocument>(
                Builders<ParkZoneDocument>.IndexKeys.Ascending(item => item.ParkId).Ascending(item => item.Slug),
                new CreateIndexOptions { Name = "idx_park_zones_park_slug" }),
            new CreateIndexModel<ParkZoneDocument>(
                Builders<ParkZoneDocument>.IndexKeys
                    .Ascending(item => item.ParkId)
                    .Ascending(item => item.SortOrder)
                    .Ascending(item => item.Name)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_park_zones_park_sort_name" }),
            new CreateIndexModel<ParkZoneDocument>(
                Builders<ParkZoneDocument>.IndexKeys.Geo2DSphere(item => item.Location),
                new CreateIndexOptions { Name = "idx_park_zones_location" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeParkItemsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkItemDocument> collection = this.database.GetCollection<ParkItemDocument>(this.settings.ParkItemsCollectionName);
        List<CreateIndexModel<ParkItemDocument>> indexes = new List<CreateIndexModel<ParkItemDocument>>
        {
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Ascending(item => item.ParkId).Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_park_items_park_name" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Ascending(item => item.ParkId).Ascending(item => item.ZoneId),
                new CreateIndexOptions { Name = "idx_park_items_park_zone" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys
                    .Ascending(item => item.ParkId)
                    .Ascending(item => item.Category)
                    .Ascending(item => item.Type)
                    .Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_park_items_park_category_type_name" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Ascending(item => item.ZoneId),
                new CreateIndexOptions { Name = "idx_park_items_zone_id" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Ascending(item => item.Category).Ascending(item => item.IsVisible).Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_park_items_category_visibility_updated" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Ascending(item => item.Category).Ascending("attractionDetails.manufacturerId"),
                new CreateIndexOptions { Name = "idx_park_items_attraction_manufacturer" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Geo2DSphere(item => item.Location),
                new CreateIndexOptions { Name = "idx_park_items_location" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeImagesIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ImageDocument> collection = this.database.GetCollection<ImageDocument>(this.settings.ImagesCollectionName);
        List<CreateIndexModel<ImageDocument>> indexes = new List<CreateIndexModel<ImageDocument>>
        {
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending(item => item.OwnerType).Ascending(item => item.OwnerId).Ascending(item => item.Category).Ascending(item => item.IsCurrent),
                new CreateIndexOptions { Name = "idx_images_owner_category_current" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending(item => item.OwnerType).Ascending(item => item.OwnerId),
                new CreateIndexOptions { Name = "idx_images_owner" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending(item => item.OwnerType).Ascending(item => item.OwnerId).Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_images_owner_created_at_desc" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys
                    .Ascending(item => item.OwnerType)
                    .Ascending(item => item.OwnerId)
                    .Ascending(item => item.Category)
                    .Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_images_owner_category_created_at_desc" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending(item => item.Category),
                new CreateIndexOptions { Name = "idx_images_category" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_images_created_at" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending("tagIds"),
                new CreateIndexOptions { Name = "idx_images_tag_ids" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeImageTagsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ImageTagDocument> collection = this.database.GetCollection<ImageTagDocument>(this.settings.ImageTagsCollectionName);
        List<CreateIndexModel<ImageTagDocument>> indexes = new List<CreateIndexModel<ImageTagDocument>>
        {
            new CreateIndexModel<ImageTagDocument>(
                Builders<ImageTagDocument>.IndexKeys.Ascending(item => item.Slug),
                new CreateIndexOptions { Name = "idx_image_tags_slug_unique", Unique = true }),
            new CreateIndexModel<ImageTagDocument>(
                Builders<ImageTagDocument>.IndexKeys.Ascending(item => item.IsActive),
                new CreateIndexOptions { Name = "idx_image_tags_is_active" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeCaptainCoasterSettingsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CaptainCoasterSettingsDocument> collection = this.database.GetCollection<CaptainCoasterSettingsDocument>(this.settings.CaptainCoasterSettingsCollectionName);
        List<CreateIndexModel<CaptainCoasterSettingsDocument>> indexes = new List<CreateIndexModel<CaptainCoasterSettingsDocument>>
        {
            new CreateIndexModel<CaptainCoasterSettingsDocument>(
                Builders<CaptainCoasterSettingsDocument>.IndexKeys.Ascending(item => item.Source),
                new CreateIndexOptions { Name = "idx_cc_settings_source_unique", Unique = true }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeCaptainCoasterParksIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CaptainCoasterParkSnapshotDocument> collection = this.database.GetCollection<CaptainCoasterParkSnapshotDocument>(this.settings.CaptainCoasterParksCollectionName);
        await this.DropIndexIfExistsAsync(collection, "idx_cc_parks_session_external_id_unique", cancellationToken);

        List<CreateIndexModel<CaptainCoasterParkSnapshotDocument>> indexes = new List<CreateIndexModel<CaptainCoasterParkSnapshotDocument>>
        {
            new CreateIndexModel<CaptainCoasterParkSnapshotDocument>(
                Builders<CaptainCoasterParkSnapshotDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.CaptainCoasterId),
                new CreateIndexOptions { Name = "idx_cc_parks_session_external_id" }),
            new CreateIndexModel<CaptainCoasterParkSnapshotDocument>(
                Builders<CaptainCoasterParkSnapshotDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_cc_parks_session_name" }),
            new CreateIndexModel<CaptainCoasterParkSnapshotDocument>(
                Builders<CaptainCoasterParkSnapshotDocument>.IndexKeys.Ascending(item => item.ScrapedAtUtc),
                new CreateIndexOptions { Name = "idx_cc_parks_scraped_at" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeCaptainCoasterCoastersIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CaptainCoasterCoasterSnapshotDocument> collection = this.database.GetCollection<CaptainCoasterCoasterSnapshotDocument>(this.settings.CaptainCoasterCoastersCollectionName);
        await this.DropIndexIfExistsAsync(collection, "idx_cc_coasters_session_external_id_unique", cancellationToken);

        List<CreateIndexModel<CaptainCoasterCoasterSnapshotDocument>> indexes = new List<CreateIndexModel<CaptainCoasterCoasterSnapshotDocument>>
        {
            new CreateIndexModel<CaptainCoasterCoasterSnapshotDocument>(
                Builders<CaptainCoasterCoasterSnapshotDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.CaptainCoasterId),
                new CreateIndexOptions { Name = "idx_cc_coasters_session_external_id" }),
            new CreateIndexModel<CaptainCoasterCoasterSnapshotDocument>(
                Builders<CaptainCoasterCoasterSnapshotDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.ParkCaptainCoasterId),
                new CreateIndexOptions { Name = "idx_cc_coasters_session_park_external_id" }),
            new CreateIndexModel<CaptainCoasterCoasterSnapshotDocument>(
                Builders<CaptainCoasterCoasterSnapshotDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.ParkName),
                new CreateIndexOptions { Name = "idx_cc_coasters_session_park_name" }),
            new CreateIndexModel<CaptainCoasterCoasterSnapshotDocument>(
                Builders<CaptainCoasterCoasterSnapshotDocument>.IndexKeys.Ascending(item => item.Manufacturer),
                new CreateIndexOptions { Name = "idx_cc_coasters_manufacturer" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeCaptainCoasterDiscoveredUrlsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CaptainCoasterDiscoveredUrlDocument> collection = this.database.GetCollection<CaptainCoasterDiscoveredUrlDocument>(this.settings.CaptainCoasterDiscoveredUrlsCollectionName);
        List<CreateIndexModel<CaptainCoasterDiscoveredUrlDocument>> indexes = new List<CreateIndexModel<CaptainCoasterDiscoveredUrlDocument>>
        {
            new CreateIndexModel<CaptainCoasterDiscoveredUrlDocument>(
                Builders<CaptainCoasterDiscoveredUrlDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.Sequence),
                new CreateIndexOptions { Name = "idx_cc_discovered_urls_session_sequence" }),
            new CreateIndexModel<CaptainCoasterDiscoveredUrlDocument>(
                Builders<CaptainCoasterDiscoveredUrlDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.CaptainCoasterId),
                new CreateIndexOptions { Name = "idx_cc_discovered_urls_session_external_id" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeCaptainCoasterSyncSessionsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CaptainCoasterSyncSessionDocument> collection = this.database.GetCollection<CaptainCoasterSyncSessionDocument>(this.settings.CaptainCoasterSyncSessionsCollectionName);
        List<CreateIndexModel<CaptainCoasterSyncSessionDocument>> indexes = new List<CreateIndexModel<CaptainCoasterSyncSessionDocument>>
        {
            new CreateIndexModel<CaptainCoasterSyncSessionDocument>(
                Builders<CaptainCoasterSyncSessionDocument>.IndexKeys.Descending(item => item.StartedAtUtc),
                new CreateIndexOptions { Name = "idx_cc_sessions_started_at_desc" }),
            new CreateIndexModel<CaptainCoasterSyncSessionDocument>(
                Builders<CaptainCoasterSyncSessionDocument>.IndexKeys.Ascending(item => item.Status).Descending(item => item.StartedAtUtc),
                new CreateIndexOptions { Name = "idx_cc_sessions_status_started_at" }),
            new CreateIndexModel<CaptainCoasterSyncSessionDocument>(
                Builders<CaptainCoasterSyncSessionDocument>.IndexKeys.Ascending(item => item.SourceKey).Descending(item => item.StartedAtUtc),
                new CreateIndexOptions { Name = "idx_cc_sessions_source_started_at" }),
            new CreateIndexModel<CaptainCoasterSyncSessionDocument>(
                Builders<CaptainCoasterSyncSessionDocument>.IndexKeys.Ascending(item => item.SourceKey).Ascending(item => item.Status).Descending(item => item.StartedAtUtc),
                new CreateIndexOptions { Name = "idx_cc_sessions_source_status_started_at" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeCaptainCoasterComparisonResultsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CaptainCoasterComparisonResultDocument> collection = this.database.GetCollection<CaptainCoasterComparisonResultDocument>(this.settings.CaptainCoasterComparisonResultsCollectionName);
        List<CreateIndexModel<CaptainCoasterComparisonResultDocument>> indexes = new List<CreateIndexModel<CaptainCoasterComparisonResultDocument>>
        {
            new CreateIndexModel<CaptainCoasterComparisonResultDocument>(
                Builders<CaptainCoasterComparisonResultDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.EntityType).Ascending(item => item.ChangeType).Ascending(item => item.DisplayName),
                new CreateIndexOptions { Name = "idx_cc_comparisons_session_entity_change_display" }),
            new CreateIndexModel<CaptainCoasterComparisonResultDocument>(
                Builders<CaptainCoasterComparisonResultDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.IsApplied),
                new CreateIndexOptions { Name = "idx_cc_comparisons_session_is_applied" }),
            new CreateIndexModel<CaptainCoasterComparisonResultDocument>(
                Builders<CaptainCoasterComparisonResultDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.ChangeType),
                new CreateIndexOptions { Name = "idx_cc_comparisons_session_change_type" }),
            new CreateIndexModel<CaptainCoasterComparisonResultDocument>(
                Builders<CaptainCoasterComparisonResultDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.ExternalEntityId),
                new CreateIndexOptions<CaptainCoasterComparisonResultDocument>
                {
                    Name = "idx_cc_comparisons_session_external_entity_id",
                    PartialFilterExpression = Builders<CaptainCoasterComparisonResultDocument>.Filter.Type(item => item.ExternalEntityId, BsonType.String),
                }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task DropIndexIfExistsAsync<TDocument>(IMongoCollection<TDocument> collection, string indexName, CancellationToken cancellationToken)
    {
        using IAsyncCursor<BsonDocument> cursor = await collection.Indexes.ListAsync(cancellationToken);
        List<BsonDocument> indexes = await cursor.ToListAsync(cancellationToken);
        bool exists = indexes.Any(item => item.TryGetValue("name", out BsonValue value) && value.AsString == indexName);
        if (exists)
        {
            await collection.Indexes.DropOneAsync(indexName, cancellationToken);
        }
    }

    private sealed class CountrySeedItem
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("isoCode")]
        public string? IsoCode { get; set; }

        [JsonPropertyName("names")]
        public List<LocalizedTextSeedItem> Names { get; set; } = new List<LocalizedTextSeedItem>();
    }

    private sealed class LocalizedTextSeedItem
    {
        [JsonPropertyName("languageCode")]
        public string? LanguageCode { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }
}
