using Microsoft.Extensions.DependencyInjection;

namespace AmusementPark.Application.DependencyInjection;

/// <summary>
/// Point d'entrée d'enregistrement de la couche Application.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Enregistre la couche Application.
    /// </summary>
    /// <param name="services">Conteneur d'injection de dépendances.</param>
    /// <returns>Le même conteneur pour chaînage.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
