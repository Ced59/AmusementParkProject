using AmusementPark.Application.Features.AdminAudit.Ports;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Ports;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.CaptainCoaster.Ports;
using AmusementPark.Application.Features.Contact.Ports;
using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.DataSources.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkDataEditorTokens.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialShare.Ports;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Application.Features.TechnicalPages.Ports;
using AmusementPark.Application.Features.TechnicalStats.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Infrastructure.Configuration.Authentication;
using AmusementPark.Infrastructure.Configuration.BackgroundJobs;
using AmusementPark.Infrastructure.Configuration.Email;
using AmusementPark.Infrastructure.Configuration.Initialization;
using AmusementPark.Infrastructure.Configuration.Images;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Configuration.Ssr;
using AmusementPark.Infrastructure.Configuration.SocialPublishing;
using AmusementPark.Infrastructure.Configuration.Videos;
using AmusementPark.Infrastructure.Configuration.Weather;
using AmusementPark.Infrastructure.Persistence.Mongo.Projections;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using AmusementPark.Infrastructure.Services.Authentication;
using AmusementPark.Infrastructure.Services.BackgroundJobs;
using AmusementPark.Infrastructure.Services.Passport;
using AmusementPark.Infrastructure.Services.Ratings;
using AmusementPark.Infrastructure.Services.DataSources;
using AmusementPark.Infrastructure.Services.DataSources.Acquisition;
using AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster;
using AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;
using AmusementPark.Infrastructure.Services.Email;
using AmusementPark.Infrastructure.Services.Comments;
using AmusementPark.Infrastructure.Services.Images;
using AmusementPark.Infrastructure.Services.Seo;
using AmusementPark.Infrastructure.Services.Ssr;
using AmusementPark.Infrastructure.Services.SocialPublishing;
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

        DurableBackgroundJobWorkerSettings durableBackgroundJobWorkerSettings =
            DurableBackgroundJobWorkerSettings.Bind(configuration);
        services.AddSingleton(durableBackgroundJobWorkerSettings);

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

        FacebookPagePublishingSettings facebookPagePublishingSettings = FacebookPagePublishingSettings.Bind(configuration);
        services.AddSingleton(facebookPagePublishingSettings);

        services.AddMemoryCache();
        services.AddHttpClient();
        services.AddHttpClient(HttpSsrPageCacheInvalidator.HttpClientName, static client =>
        {
            client.Timeout = TimeSpan.FromSeconds(3);
        });
        services.AddHttpClient(HttpTechnicalStatsProvider.HttpClientName, static client =>
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
        services.AddHttpClient(FacebookPageSocialPublisher.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(facebookPagePublishingSettings.RequestTimeoutSeconds);
        });
        services.AddHttpClient(FacebookPageSocialPublisher.PreviewRefreshHttpClientName, static client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient(FacebookPageSocialPublisher.PreviewPagePreparationHttpClientName, static client =>
        {
            client.Timeout = TimeSpan.FromSeconds(45);
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

        services.AddScoped<IDurableBackgroundJobRepository, DurableBackgroundJobRepository>();
        services.AddSingleton<DurableBackgroundJobMetrics>();
        services.AddHostedService<DurableBackgroundJobWorkerBackgroundService>();
        services.AddHostedService<RatingRankingRebuildReconciliationBackgroundService>();

        services.AddScoped<ICountryReadRepository, CountryReadRepository>();
        services.AddScoped<IParkFounderRepository, ParkFounderRepository>();
        services.AddScoped<IParkOperatorRepository, ParkOperatorRepository>();
        services.AddScoped<IAttractionManufacturerRepository, AttractionManufacturerRepository>();
        services.AddScoped<ITechnicalPageRepository, TechnicalPageRepository>();
        services.AddScoped<IParkRepository, ParkRepository>();
        services.AddScoped<IParkNameReadRepository, ParkNameReadRepository>();
        services.AddScoped<IParkDetailSummaryReadRepository, ParkDetailSummaryReadRepository>();
        services.AddScoped<IParkMapItemsReadRepository, ParkMapItemsReadRepository>();
        services.AddScoped<IParkZoneRepository, ParkZoneRepository>();
        services.AddScoped<IParkItemRepository, ParkItemRepository>();
        services.AddScoped<IParkItemNameReadRepository, ParkItemNameReadRepository>();
        services.AddScoped<IVisitTargetReadRepository, VisitTargetReadRepository>();
        services.AddScoped<IStandaloneAttractionRepository, StandaloneAttractionRepository>();
        services.AddScoped<IAttractionAccessConditionTypeDefinitionRepository, AttractionAccessConditionTypeDefinitionRepository>();
        services.AddScoped<ISearchReadRepository, SearchReadRepository>();
        services.AddScoped<IImageRepository, ImageRepository>();
        services.AddScoped<IImageTagRepository, ImageTagRepository>();
        services.AddScoped<IImageProcessingPipeline, ImageMetadataPipeline>();
        services.AddScoped<IImageVariantGenerationLease, MongoImageVariantGenerationLease>();
        services.AddScoped<IImageBinaryStorage, MinioImageBinaryStorage>();
        services.AddScoped<IRemoteImageImporter, RemoteImageImporter>();
        services.AddScoped<IVideoRepository, VideoRepository>();
        services.AddScoped<IVideoTagRepository, VideoTagRepository>();
        services.AddScoped<IVideoMetadataProvider, ExternalVideoMetadataProvider>();
        services.AddScoped<IVideoThumbnailImporter, VideoThumbnailImporter>();
        services.AddScoped<IContactGrievanceRepository, ContactGrievanceRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ICommentContentSanitizer, CommentContentSanitizer>();
        services.AddHostedService<CommentImageDraftCleanupBackgroundService>();
        services.AddScoped<ISocialShareEventRepository, SocialShareEventRepository>();
        services.AddScoped<ISocialPublicationRepository, SocialPublicationRepository>();
        services.AddScoped<ISocialPublisher, FacebookPageSocialPublisher>();
        services.AddScoped<ISocialWebhookHandler, FacebookPageWebhookHandler>();
        services.AddScoped<IRatingRepository, RatingRepository>();
        services.AddScoped<IRatingEvidenceReader, RatingEvidenceReader>();
        services.AddScoped<IRatingDiagnosticsReader, RatingDiagnosticsReader>();
        services.AddScoped<IRankingSnapshotRepository, RankingSnapshotRepository>();
        services.AddScoped<IRatingRankingSourceRevisionRepository, RatingRankingSourceRevisionRepository>();
        services.AddScoped<IUserRankingShareRepository, UserRankingShareRepository>();
        services.AddScoped<IUserVisitRepository, UserVisitRepository>();
        services.AddScoped<IRideOccurrenceRepository, UserRideOccurrenceRepository>();
        services.AddScoped<IPassportExportRepository, PassportExportRepository>();
        services.AddScoped<IVisitDeletionStore, MongoVisitDeletionStore>();
        services.AddScoped<IPassportItemStatisticsSourceReader,
            PassportItemStatisticsSourceReader>();
        services.AddScoped<IPassportScopeStatisticsSourceReader,
            PassportScopeStatisticsSourceReader>();
        services.AddScoped<IPassportBetaMetricsSource,
            PassportBetaMetricsSource>();
        services.AddScoped<IGlobalRatingSuggestionSourceReader,
            GlobalRatingSuggestionSourceReader>();
        services.AddScoped<IGlobalRatingSuggestionStateRepository,
            GlobalRatingSuggestionStateRepository>();
        services.AddScoped<IGlobalRatingSuggestionAnalyticsOutboxReconciler,
            GlobalRatingSuggestionStateRepository>();
        services.AddHostedService<GlobalRatingSuggestionAnalyticsOutboxBackgroundService>();
        services.AddScoped<PassportAuditStore>();
        services.AddScoped<IPassportAuditPublisher>(serviceProvider =>
            serviceProvider.GetRequiredService<PassportAuditStore>());
        services.AddScoped<IPassportAuditReconciler>(serviceProvider =>
            serviceProvider.GetRequiredService<PassportAuditStore>());
        services.AddScoped<IVisitContentMutationLeaseManager, MongoVisitContentMutationLeaseManager>();
        services.AddHostedService<PassportAuditReconciliationBackgroundService>();
        services.AddHostedService<PassportExportReconciliationBackgroundService>();
        services.AddHostedService<VisitDeletionReconciliationBackgroundService>();
        services.AddSingleton<IPassportClock, SystemPassportClock>();
        services.AddSingleton<IPassportTimeZoneValidator, SystemPassportTimeZoneValidator>();
        services.AddSingleton<IPassportLocalDateResolver, SystemPassportLocalDateResolver>();
        services.AddSingleton<IRatingRankSnapshotCache, InMemoryRatingRankSnapshotCache>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IParkDataEditorAccessTokenRepository, ParkDataEditorAccessTokenRepository>();
        services.AddScoped<IAdminAuditLogWriter, AdminAuditLogWriter>();
        services.AddScoped<IAdminAuditLogReader, AdminAuditLogReader>();
        services.AddScoped<ISeoSitemapSnapshotRepository, SeoSitemapSnapshotRepository>();
        services.AddScoped<ISeoSitemapGenerationHistoryRepository, SeoSitemapGenerationHistoryRepository>();
        services.AddScoped<ISeoSitemapSettingsRepository, SeoSitemapSettingsRepository>();
        services.AddScoped<IIndexNowSubmitter, IndexNowSubmitter>();
        services.AddSingleton<InMemorySeoSitemapRefreshScheduler>();
        services.AddSingleton<ISeoSitemapRefreshScheduler>(serviceProvider => serviceProvider.GetRequiredService<InMemorySeoSitemapRefreshScheduler>());
        services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<InMemorySeoSitemapRefreshScheduler>());
        services.AddHostedService<ParkPricingSitemapRolloverRefreshService>();
        services.AddScoped<ISsrPageCacheInvalidator, HttpSsrPageCacheInvalidator>();
        services.AddScoped<IParkGraphUpsertHistoryRepository, ParkGraphUpsertHistoryRepository>();
        services.AddScoped<IParkWeatherRepository, ParkWeatherRepository>();
        services.AddScoped<IParkWeatherRunRepository, ParkWeatherRunRepository>();
        services.AddScoped<IParkOpeningHoursRepository, ParkOpeningHoursRepository>();
        services.AddScoped<IParkPricingRepository, ParkPricingRepository>();
        services.AddScoped<IHistoryEventRepository, HistoryEventRepository>();
        services.AddScoped<IParkWeatherProviderStrategy, OpenMeteoWeatherProviderStrategy>();
        services.AddScoped<IParkWeatherProviderStrategyResolver, ParkWeatherProviderStrategyResolver>();
        services.AddSingleton<IParkWeatherRefreshQueue, ParkWeatherRefreshQueue>();
        services.AddHostedService<ParkWeatherRefreshBackgroundService>();
        services.AddHostedService<ParkWeatherAutomaticRefreshBackgroundService>();
        services.AddScoped<ICaptainCoasterSettingsRepository, CaptainCoasterSettingsRepository>();
        services.AddScoped<ITechnicalStatsProvider, HttpTechnicalStatsProvider>();
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
        services.AddSingleton<IUserRankingShareIdFactory, UserRankingShareIdFactory>();
        services.AddSingleton<IUserRankingSharePreviewRenderer, UserRankingSharePreviewRenderer>();
        services.AddSingleton<IParkDataEditorTokenProtector, ParkDataEditorTokenProtector>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<ILocalAccountEmailService, LocalAccountEmailService>();
        services.AddScoped<IExternalIdentityVerifier, GoogleExternalIdentityVerifier>();
        services.AddScoped<IUserAvatarImporter, UserAvatarImporter>();
        services.AddSingleton<BrandedEmailTemplateRenderer>();
        services.AddScoped<IContactNotificationService, ContactNotificationEmailService>();
        services.AddScoped<IParkWeatherNotificationService, ParkWeatherNotificationEmailService>();
        services.AddScoped<IParkOpeningHoursNotificationService, ParkOpeningHoursNotificationEmailService>();
        services.AddHostedService<ParkOpeningHoursCoverageNotificationBackgroundService>();

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
