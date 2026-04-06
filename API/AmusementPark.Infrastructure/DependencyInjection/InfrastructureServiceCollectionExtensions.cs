using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        return services;
    }
}
