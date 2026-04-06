using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace AmusementPark.Application.DependencyInjection;

/// <summary>
/// Point d'entrée d'enregistrement de la couche Application.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Enregistre la couche Application en mode minimal.
    /// </summary>
    /// <remarks>
    /// Cette surcharge n'enregistre volontairement pas tous les handlers automatiquement.
    /// Pendant la migration progressive du legacy vers la Clean Architecture,
    /// seuls les services applicatifs sans dépendances Infrastructure obligatoires
    /// doivent être activés explicitement via <see cref="AddApplicationHandlers"/>.
    /// </remarks>
    /// <param name="services">Conteneur d'injection de dépendances.</param>
    /// <returns>Le même conteneur pour chaînage.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<PagedQueryValidator>();
        services.AddSingleton<IApplicationValidator<AmusementPark.Application.Common.Requests.PagedQuery>, PagedQueryValidator>();
        services.AddScoped<ParkItemReferenceValidator>();
        return services;
    }

    /// <summary>
    /// Enregistre explicitement les handlers Application sélectionnés.
    /// </summary>
    /// <param name="services">Conteneur d'injection de dépendances.</param>
    /// <param name="predicate">Filtre optionnel permettant d'activer seulement certains handlers.</param>
    /// <returns>Le même conteneur pour chaînage.</returns>
    public static IServiceCollection AddApplicationHandlers(this IServiceCollection services, Func<Type, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        RegisterHandlers(services, typeof(ApplicationServiceCollectionExtensions).Assembly, predicate);
        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly, Func<Type, bool>? predicate)
    {
        IEnumerable<Type> implementationTypes = assembly
            .GetTypes()
            .Where(static type => type is { IsAbstract: false, IsInterface: false })
            .Where(type => predicate == null || predicate(type));

        foreach (Type implementationType in implementationTypes)
        {
            IEnumerable<Type> serviceTypes = implementationType
                .GetInterfaces()
                .Where(static type => type.IsGenericType)
                .Where(static type =>
                    type.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                    type.GetGenericTypeDefinition() == typeof(IQueryHandler<,>) ||
                    type.GetGenericTypeDefinition() == typeof(IApplicationValidator<>));

            foreach (Type serviceType in serviceTypes)
            {
                services.AddScoped(serviceType, implementationType);
            }
        }
    }
}
