using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.CaptainCoaster.Ports;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Infrastructure.Configuration.Images;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Projections;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
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
    /// <param name="services">Conteneur d'injection de dépendances.</param>
    /// <param name="configuration">Configuration applicative.</param>
    /// <returns>Le même conteneur pour chaînage.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        MongoDbSettings mongoDbSettings = MongoDbSettings.Bind(configuration);
        services.AddSingleton(mongoDbSettings);

        MinioImageStorageSettings minioSettings = configuration.GetSection(MinioImageStorageSettings.SectionName).Get<MinioImageStorageSettings>() ?? new MinioImageStorageSettings();
        services.AddSingleton(minioSettings);

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
        services.AddScoped<ISearchProjectionWriter, MongoSearchProjectionWriter>();

        return services;
    }
}
