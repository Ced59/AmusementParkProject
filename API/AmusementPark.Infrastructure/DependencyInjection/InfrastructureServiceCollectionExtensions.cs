using AmusementPark.Application.Features.AdminAudit.Ports;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Ports;
using AmusementPark.Application.Features.CaptainCoaster.Ports;
using AmusementPark.Application.Features.Contact.Ports;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.DataSources.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialShare.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Infrastructure.Configuration.Authentication;
using AmusementPark.Infrastructure.Configuration.Email;
using AmusementPark.Infrastructure.Configuration.Initialization;
using AmusementPark.Infrastructure.Configuration.Images;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Configuration.Ssr;
using AmusementPark.Infrastructure.Configuration.Videos;
using AmusementPark.Infrastructure.Configuration.Weather;
using AmusementPark.Infrastructure.Persistence.Mongo.Projections;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using AmusementPark.Infrastructure.Services.Authentication;
using AmusementPark.Infrastructure.Services.DataSources;
using AmusementPark.Infrastructure.Services.DataSources.Acquisition;
using AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster;
using AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;
using AmusementPark.Infrastructure.Services.Email;
using AmusementPark.Infrastructure.Services.Images;
using AmusementPark.Infrastructure.Services.Seo;
using AmusementPark.Infrastructure.Services.Ssr;
using AmusementPark.Infrastructure.Services.Videos;
using AmusementPark.Infrastructure.Services.Weather;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.DependencyInjection;

/// <summary>
/// Point d'entrée d'enregistrement de la couche Infrastructure.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Enregistre la couche Infrastructure.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        MongoDbSettings mongoDbSettings = MongoDbSettings.Bind(configuration);
        services.AddSingleton(mongoDbSettings);

        MinioImageStorageSettings minioSettings = configuration.GetSection(MinioImageStorageSettings.SectionName).Get<MinioImageStorageSettings>() ?? new MinioImageStorageSettings();
        services.AddSingleton(minioSettings);

        VideoMetadataSettings videoMetadataSettings = VideoMetadataSettings.Bind(configuration);
        services.AddSingleton(videoMetadataSettings);

        ParkWeatherSettings parkWeatherSettings = ParkWeatherSettings.Bind(configuration);
        services.AddSingleton(parkWeatherSettings);
        services.AddSingleton<IParkWeatherRefreshSettings>(parkWeatherSettings);

        JwtSettings jwtSettings = configuration.GetSection("Authentication:Jwt").Get<JwtSettings>() ?? new JwtSettings();
        services.AddSingleton(jwtSettings);

        EmailSettings emailSettings = configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
        services.AddSingleton(emailSettings);

        EmailNotificationSettings emailNotificationSettings = EmailNotificationSettings.Bind(configuration);
        services.AddSingleton(emailNotificationSettings);

        GoogleOAuthSettings googleOAuthSettings = configuration.GetSection("Authentication:Google").Get<GoogleOAuthSettings>() ?? new GoogleOAuthSettings();
        services.AddSingleton(googleOAuthSettings);

        UserAuthenticationSettings userAuthenticationSettings = UserAuthenticationSettings.Bind(configuration);
        services.AddSingleton<IUserAuthenticationSettings>(userAuthenticationSettings);

        AdminSeedSettings adminSeedSettings = configuration.GetSection("Initialization:AdminUser").Get<AdminSeedSettings>() ?? new AdminSeedSettings();
        services.AddSingleton(adminSeedSettings);

        SsrSettings ssrSettings = configuration.GetSection(SsrSettings.SectionName).Get<SsrSettings>() ?? new SsrSettings();
        services.AddSingleton(ssrSettings);

        services.AddMemoryCache();
        services.AddHttpClient();
        services.AddHttpClient(HttpSsrPageCacheInvalidator.HttpClientName, static client =>
        {
            client.Timeout = TimeSpan.FromSeconds(3);
        });
        services.AddHttpClient(ExternalVideoMetadataProvider.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(videoMetadataSettings.RequestTimeoutSeconds);
        });
        services.AddHttpClient(OpenMeteoWeatherProviderStrategy.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(parkWeatherSettings.RequestTimeoutSeconds);
        });
        services.AddHttpClient(RemoteImageImporter.HttpClientName, static client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .ConfigurePrimaryHttpMessageHandler(static () => new HttpClientHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
        });
        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoDbSettings.Url));
        services.AddSingleton<IMinioClient>(_ =>
            new MinioClient()
                .WithEndpoint(minioSettings.Endpoint)
                .WithCredentials(minioSettings.AccessKey, minioSettings.SecretKey)
                .WithSSL(minioSettings.WithSsl)
                .Build());

        services.AddScoped<IMongoDatabase>(serviceProvider =>
        {
            IMongoClient client = serviceProvider.GetRequiredService<IMongoClient>();
            return client.GetDatabase(mongoDbSettings.DatabaseName);
        });

        services.AddScoped<ICountryReadRepository, CountryReadRepository>();
        services.AddScoped<IParkFounderRepository, ParkFounderRepository>();
        services.AddScoped<IParkOperatorRepository, ParkOperatorRepository>();
        services.AddScoped<IAttractionManufacturerRepository, AttractionManufacturerRepository>();
        services.AddScoped<IParkRepository, ParkRepository>();
        services.AddScoped<IParkDetailSummaryReadRepository, ParkDetailSummaryReadRepository>();
        services.AddScoped<IParkMapItemsReadRepository, ParkMapItemsReadRepository>();
        services.AddScoped<IParkZoneRepository, ParkZoneRepository>();
        services.AddScoped<IParkItemRepository, ParkItemRepository>();
        services.AddScoped<IAttractionAccessConditionTypeDefinitionRepository, AttractionAccessConditionTypeDefinitionRepository>();
        services.AddScoped<ISearchReadRepository, SearchReadRepository>();
        services.AddScoped<IImageRepository, ImageRepository>();
        services.AddScoped<IImageTagRepository, ImageTagRepository>();
        services.AddScoped<IImageProcessingPipeline, ImageMetadataPipeline>();
        services.AddScoped<IImageBinaryStorage, MinioImageBinaryStorage>();
        services.AddScoped<IRemoteImageImporter, RemoteImageImporter>();
        services.AddScoped<IVideoRepository, VideoRepository>();
        services.AddScoped<IVideoTagRepository, VideoTagRepository>();
        services.AddScoped<IVideoMetadataProvider, ExternalVideoMetadataProvider>();
        services.AddScoped<IVideoThumbnailImporter, VideoThumbnailImporter>();
        services.AddScoped<IContactGrievanceRepository, ContactGrievanceRepository>();
        services.AddScoped<ISocialShareEventRepository, SocialShareEventRepository>();
        services.AddScoped<IRatingRepository, RatingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAdminAuditLogWriter, AdminAuditLogWriter>();
        services.AddScoped<IAdminAuditLogReader, AdminAuditLogReader>();
        services.AddScoped<ISeoSitemapSnapshotRepository, SeoSitemapSnapshotRepository>();
        services.AddScoped<ISeoSitemapGenerationHistoryRepository, SeoSitemapGenerationHistoryRepository>();
        services.AddScoped<ISeoSitemapSettingsRepository, SeoSitemapSettingsRepository>();
        services.AddScoped<IIndexNowSubmitter, IndexNowSubmitter>();
        services.AddSingleton<InMemorySeoSitemapRefreshScheduler>();
        services.AddSingleton<ISeoSitemapRefreshScheduler>(serviceProvider => serviceProvider.GetRequiredService<InMemorySeoSitemapRefreshScheduler>());
        services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<InMemorySeoSitemapRefreshScheduler>());
        services.AddScoped<ISsrPageCacheInvalidator, HttpSsrPageCacheInvalidator>();
        services.AddScoped<IParkGraphUpsertHistoryRepository, ParkGraphUpsertHistoryRepository>();
        services.AddScoped<IParkWeatherRepository, ParkWeatherRepository>();
        services.AddScoped<IParkWeatherRunRepository, ParkWeatherRunRepository>();
        services.AddScoped<IParkWeatherProviderStrategy, OpenMeteoWeatherProviderStrategy>();
        services.AddScoped<IParkWeatherProviderStrategyResolver, ParkWeatherProviderStrategyResolver>();
        services.AddSingleton<IParkWeatherRefreshQueue, ParkWeatherRefreshQueue>();
        services.AddHostedService<ParkWeatherRefreshBackgroundService>();
        services.AddHostedService<ParkWeatherAutomaticRefreshBackgroundService>();
        services.AddScoped<ICaptainCoasterSettingsRepository, CaptainCoasterSettingsRepository>();
        services.AddScoped<ICaptainCoasterSessionRepository, CaptainCoasterSessionRepository>();
        services.AddSingleton<IDataSourceImportJobQueue, InMemoryDataSourceImportJobQueue>();
        services.AddScoped<IDataAcquisitionHttpFetcher, DataAcquisitionHttpFetcher>();
        services.AddScoped<IXmlSitemapUrlDiscoveryService, XmlSitemapUrlDiscoveryService>();
        services.AddScoped<ICaptainCoasterCoasterPageParser, CaptainCoasterCoasterPageParser>();
        services.AddScoped<ICaptainCoasterMapPageParser, CaptainCoasterMapPageParser>();
        services.AddScoped<IDataSourceImportJobProcessor, DataSourceImportJobProcessor>();
        services.AddScoped<IDataSourceProvider, CaptainCoasterDataSourceProvider>();
        services.AddScoped<IDataSourceAdministrationService, DataSourceAdministrationService>();
        services.AddHostedService<DataSourceImportBackgroundService>();
        services.AddScoped<ISearchProjectionWriter, MongoSearchProjectionWriter>();

        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IRefreshTokenFactory, LocalAccountTokenFactory>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<ILocalAccountEmailService, LocalAccountEmailService>();
        services.AddScoped<IExternalIdentityVerifier, GoogleExternalIdentityVerifier>();
        services.AddScoped<IUserAvatarImporter, UserAvatarImporter>();
        services.AddSingleton<BrandedEmailTemplateRenderer>();
        services.AddScoped<IContactNotificationService, ContactNotificationEmailService>();
        services.AddScoped<IParkWeatherNotificationService, ParkWeatherNotificationEmailService>();

        if (string.Equals(emailSettings.Mode, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, ConsoleEmailSender>();
        }

        return services;
    }
}
