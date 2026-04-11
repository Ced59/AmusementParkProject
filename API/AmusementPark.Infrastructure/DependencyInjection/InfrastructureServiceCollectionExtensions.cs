using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.CaptainCoaster.Ports;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.DataSources.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Infrastructure.Configuration.Authentication;
using AmusementPark.Infrastructure.Configuration.Initialization;
using AmusementPark.Infrastructure.Configuration.Images;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Projections;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using AmusementPark.Infrastructure.Services.Authentication;
using AmusementPark.Infrastructure.Services.DataSources;
using AmusementPark.Infrastructure.Services.DataSources.Acquisition;
using AmusementPark.Infrastructure.Services.DataSources.CaptainCoasterScraping;
using AmusementPark.Infrastructure.Services.Images;
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

        JwtSettings jwtSettings = configuration.GetSection("Authentication:Jwt").Get<JwtSettings>() ?? new JwtSettings();
        services.AddSingleton(jwtSettings);

        EmailSettings emailSettings = configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
        services.AddSingleton(emailSettings);

        GoogleOAuthSettings googleOAuthSettings = configuration.GetSection("Authentication:Google").Get<GoogleOAuthSettings>() ?? new GoogleOAuthSettings();
        services.AddSingleton(googleOAuthSettings);

        UserAuthenticationSettings userAuthenticationSettings = UserAuthenticationSettings.Bind(configuration);
        services.AddSingleton<IUserAuthenticationSettings>(userAuthenticationSettings);

        AdminSeedSettings adminSeedSettings = configuration.GetSection("Initialization:AdminUser").Get<AdminSeedSettings>() ?? new AdminSeedSettings();
        services.AddSingleton(adminSeedSettings);

        services.AddHttpClient();
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
        services.AddScoped<IParkZoneRepository, ParkZoneRepository>();
        services.AddScoped<IParkItemRepository, ParkItemRepository>();
        services.AddScoped<ISearchReadRepository, SearchReadRepository>();
        services.AddScoped<IImageRepository, ImageRepository>();
        services.AddScoped<IImageTagRepository, ImageTagRepository>();
        services.AddScoped<IImageProcessingPipeline, ImageMetadataPipeline>();
        services.AddScoped<IImageBinaryStorage, MinioImageBinaryStorage>();
        services.AddScoped<IUserRepository, UserRepository>();
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
