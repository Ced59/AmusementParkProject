using AmusementPark.Infrastructure.Persistence.Mongo.Initialization;
using AmusementPark.Infrastructure.Persistence.Mongo.Projections;

namespace AmusementPark.WebAPI.DependencyInjection;

/// <summary>
/// Enregistre et exécute les initialiseurs Mongo nécessaires au démarrage.
/// </summary>
public static class MongoInitializationServiceCollectionExtensions
{
    public static IServiceCollection AddMongoInitialization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<MongoDatabaseInitializer>();
        services.AddScoped<MongoSearchProjectionInitializer>();

        return services;
    }

    public static async Task InitializeMongoAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        using IServiceScope scope = app.Services.CreateScope();
        MongoDatabaseInitializer mongoDatabaseInitializer = scope.ServiceProvider.GetRequiredService<MongoDatabaseInitializer>();
        await mongoDatabaseInitializer.InitializeAsync(cancellationToken);

        MongoSearchProjectionInitializer searchProjectionInitializer = scope.ServiceProvider.GetRequiredService<MongoSearchProjectionInitializer>();
        await searchProjectionInitializer.InitializeAsync(cancellationToken);
    }
}
