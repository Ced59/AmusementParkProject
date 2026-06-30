using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.AttractionAccessConditionTypes;
using AmusementPark.Infrastructure.Configuration.Initialization;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Countries;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.TechnicalPages;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Videos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

/// <summary>
/// Initialise les collections MongoDB métier, leurs index et les seeds de base.
/// </summary>
public sealed partial class MongoDatabaseInitializer
{
    private readonly IMongoDatabase database;
    private readonly MongoDbSettings settings;
    private readonly AdminSeedSettings adminSeedSettings;
    private readonly IHostEnvironment hostEnvironment;
    private readonly ILogger<MongoDatabaseInitializer> logger;

    public MongoDatabaseInitializer(
        IMongoDatabase database,
        MongoDbSettings settings,
        AdminSeedSettings adminSeedSettings,
        IHostEnvironment hostEnvironment,
        ILogger<MongoDatabaseInitializer> logger)
    {
        this.database = database;
        this.settings = settings;
        this.adminSeedSettings = adminSeedSettings;
        this.hostEnvironment = hostEnvironment;
        this.logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await this.EnsureCollectionExistsAsync(this.settings.UsersCollectionName, cancellationToken);
        await this.InitializeUsersIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.RefreshTokensCollectionName, cancellationToken);
        await this.InitializeRefreshTokensIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ImagesCollectionName, cancellationToken);
        await this.BackfillLegacyImageCategoriesAsync(cancellationToken);
        await this.InitializeImagesIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ImageTagsCollectionName, cancellationToken);
        await this.InitializeImageTagsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.VideosCollectionName, cancellationToken);
        await this.InitializeVideosIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.VideoTagsCollectionName, cancellationToken);
        await this.InitializeVideoTagsIndexesAsync(cancellationToken);
        await this.SeedSystemVideoTagsAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ContactGrievancesCollectionName, cancellationToken);
        await this.InitializeContactGrievanceIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.SocialShareEventsCollectionName, cancellationToken);
        await this.InitializeSocialShareEventIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.UserRatingsCollectionName, cancellationToken);
        await this.EnsureCollectionExistsAsync(this.settings.RatingAggregatesCollectionName, cancellationToken);
        await this.InitializeRatingsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.CountriesCollectionName, cancellationToken);
        await this.InitializeCountriesIndexesAsync(cancellationToken);
        await this.SeedCountriesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParksCollectionName, cancellationToken);
        await this.InitializeParksIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParkOpeningHoursCollectionName, cancellationToken);
        await this.InitializeParkOpeningHoursIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.HistoryEventsCollectionName, cancellationToken);
        await this.InitializeHistoryEventsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParkFoundersCollectionName, cancellationToken);
        await this.InitializeParkFoundersIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParkOperatorsCollectionName, cancellationToken);
        await this.InitializeParkOperatorsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.AttractionManufacturersCollectionName, cancellationToken);
        await this.InitializeAttractionManufacturersIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.TechnicalPagesCollectionName, cancellationToken);
        await this.InitializeTechnicalPagesIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParkZonesCollectionName, cancellationToken);
        await this.InitializeParkZonesIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParkItemsCollectionName, cancellationToken);
        await this.InitializeParkItemsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(AdminFieldModeItemProgressCollectionName, cancellationToken);
        await this.InitializeAdminFieldModeItemProgressAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.AttractionAccessConditionTypesCollectionName, cancellationToken);
        await this.InitializeAttractionAccessConditionTypesIndexesAsync(cancellationToken);
        await this.SeedSystemAttractionAccessConditionTypesAsync(cancellationToken);

        await this.BackfillAdminReviewPrioritiesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.SearchItemCollectionName, cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.AdminAuditLogsCollectionName, cancellationToken);
        await this.InitializeAdminAuditIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.SeoSitemapSnapshotsCollectionName, cancellationToken);
        await this.EnsureCollectionExistsAsync(this.settings.SeoSitemapSnapshotSectionsCollectionName, cancellationToken);
        await this.EnsureCollectionExistsAsync(this.settings.SeoSitemapGenerationHistoryCollectionName, cancellationToken);
        await this.EnsureCollectionExistsAsync(this.settings.SeoSitemapSettingsCollectionName, cancellationToken);
        await this.InitializeSeoSitemapIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParkGraphUpsertHistoryCollectionName, cancellationToken);
        await this.InitializeParkGraphUpsertHistoryIndexesAsync(cancellationToken);

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

        await this.EnsureCollectionExistsAsync(this.settings.ParkWeatherDailySnapshotsCollectionName, cancellationToken);
        await this.EnsureCollectionExistsAsync(this.settings.ParkWeatherRunsCollectionName, cancellationToken);
        await this.EnsureCollectionExistsAsync(this.settings.ParkWeatherRunItemsCollectionName, cancellationToken);
        await this.InitializeParkWeatherIndexesAsync(cancellationToken);

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
}
