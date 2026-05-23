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

    public string SearchItemCollectionName { get; set; } = "searchItems";

    public string AdminAuditLogsCollectionName { get; set; } = "adminAuditLogs";

    public string ImagesCollectionName { get; set; } = "images";

    public string ImageTagsCollectionName { get; set; } = "imageTags";

    public string CountriesCollectionName { get; set; } = "countries";

    public string CaptainCoasterSettingsCollectionName { get; set; } = "captainCoasterSettings";

    public string CaptainCoasterParksCollectionName { get; set; } = "captainCoasterParks";

    public string CaptainCoasterCoastersCollectionName { get; set; } = "captainCoasterCoasters";

    public string CaptainCoasterDiscoveredUrlsCollectionName { get; set; } = "captainCoasterDiscoveredUrls";

    public string CaptainCoasterSyncSessionsCollectionName { get; set; } = "captainCoasterSyncSessions";

    public string CaptainCoasterComparisonResultsCollectionName { get; set; } = "captainCoasterComparisonResults";

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

        return settings;
    }
}
