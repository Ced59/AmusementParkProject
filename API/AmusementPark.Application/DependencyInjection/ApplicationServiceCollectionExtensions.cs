using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Validation;
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

        services.AddSingleton<PagedQueryValidator>();
        RegisterHandlers(services, typeof(ApplicationServiceCollectionExtensions).Assembly);
        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        IEnumerable<Type> implementationTypes = assembly
            .GetTypes()
            .Where(static type => type is { IsAbstract: false, IsInterface: false });

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
