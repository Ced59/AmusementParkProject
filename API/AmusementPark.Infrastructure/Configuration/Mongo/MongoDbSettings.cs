using Microsoft.Extensions.Configuration;

namespace AmusementPark.Infrastructure.Configuration.Mongo;

/// <summary>
/// Paramètres de connexion et de collections MongoDB de l'application.
/// </summary>
public sealed class MongoDbSettings
{
    /// <summary>
    /// Nom de la section de configuration.
    /// </summary>
    public const string SectionName = "MongoDB";

    public string Url { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string UsersCollectionName { get; set; } = "users";

    public string RefreshTokensCollectionName { get; set; } = "refreshTokens";

    public string ParksCollectionName { get; set; } = "parks";

    public string ParkFoundersCollectionName { get; set; } = "parkFounders";

    public string ParkOperatorsCollectionName { get; set; } = "parkOperators";

    public string AttractionManufacturersCollectionName { get; set; } = "attractionManufacturers";

    public string ParkZonesCollectionName { get; set; } = "parkZones";

    public string ParkItemsCollectionName { get; set; } = "parkItems";

    public string AttractionAccessConditionTypesCollectionName { get; set; } = "attractionAccessConditionTypes";

    public string SearchItemCollectionName { get; set; } = "searchItems";

    /// <summary>
    /// Reconstruit explicitement la projection de recherche au démarrage.
    /// Par défaut, la projection n'est reconstruite que si elle est vide afin d'éviter
    /// un gros volume d'upserts Mongo à chaque redéploiement.
    /// </summary>
    public bool RebuildSearchProjectionOnStartup { get; set; }

    /// <summary>
    /// Taille des lots utilisés lors d'une reconstruction volontaire de la projection de recherche.
    /// </summary>
    public int SearchProjectionRebuildBatchSize { get; set; } = 250;

    /// <summary>
    /// Pause optionnelle entre deux lots de reconstruction de la projection de recherche.
    /// Utile sur un petit VPS bridé CPU pour lisser les écritures Mongo.
    /// </summary>
    public int SearchProjectionRebuildBatchDelayMilliseconds { get; set; }

    public string AdminAuditLogsCollectionName { get; set; } = "adminAuditLogs";

    public string SeoSitemapSnapshotsCollectionName { get; set; } = "seoSitemapSnapshots";

    public string SeoSitemapGenerationHistoryCollectionName { get; set; } = "seoSitemapGenerationHistory";

    public string SeoSitemapSettingsCollectionName { get; set; } = "seoSitemapSettings";

    public string ParkGraphUpsertHistoryCollectionName { get; set; } = "parkGraphUpsertHistory";

    public int ParkGraphUpsertHistoryRetentionDays { get; set; } = 30;

    public string ImagesCollectionName { get; set; } = "images";

    public string ImageTagsCollectionName { get; set; } = "imageTags";

    public string VideosCollectionName { get; set; } = "videos";

    public string VideoTagsCollectionName { get; set; } = "videoTags";

    public string ContactGrievancesCollectionName { get; set; } = "contactGrievances";

    public string SocialShareEventsCollectionName { get; set; } = "socialShareEvents";

    public string UserRatingsCollectionName { get; set; } = "userRatings";

    public string RatingAggregatesCollectionName { get; set; } = "ratingAggregates";

    public string CountriesCollectionName { get; set; } = "countries";

    public string CaptainCoasterSettingsCollectionName { get; set; } = "captainCoasterSettings";

    public string CaptainCoasterParksCollectionName { get; set; } = "captainCoasterParks";

    public string CaptainCoasterCoastersCollectionName { get; set; } = "captainCoasterCoasters";

    public string CaptainCoasterDiscoveredUrlsCollectionName { get; set; } = "captainCoasterDiscoveredUrls";

    public string CaptainCoasterSyncSessionsCollectionName { get; set; } = "captainCoasterSyncSessions";

    public string CaptainCoasterComparisonResultsCollectionName { get; set; } = "captainCoasterComparisonResults";

    public string ParkWeatherDailySnapshotsCollectionName { get; set; } = "parkWeatherDailySnapshots";

    public string ParkWeatherRunsCollectionName { get; set; } = "parkWeatherRuns";

    public string ParkWeatherRunItemsCollectionName { get; set; } = "parkWeatherRunItems";

    /// <summary>
    /// Lie la configuration et applique des valeurs par défaut.
    /// </summary>
    public static MongoDbSettings Bind(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        MongoDbSettings settings = configuration.GetSection(SectionName).Get<MongoDbSettings>() ?? new MongoDbSettings();

        if (string.IsNullOrWhiteSpace(settings.DatabaseName))
        {
            settings.DatabaseName = "AmusementPark";
        }

        if (settings.SearchProjectionRebuildBatchSize <= 0)
        {
            settings.SearchProjectionRebuildBatchSize = 250;
        }

        if (settings.SearchProjectionRebuildBatchDelayMilliseconds < 0)
        {
            settings.SearchProjectionRebuildBatchDelayMilliseconds = 0;
        }

        if (settings.ParkGraphUpsertHistoryRetentionDays <= 0)
        {
            settings.ParkGraphUpsertHistoryRetentionDays = 30;
        }

        return settings;
    }
}
