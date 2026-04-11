using System;
using AmusementPark.Application.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace AmusementPark.WebAPI.DependencyInjection;

/// <summary>
/// Regroupe l'enregistrement des handlers applicatifs activés pour l'API.
/// </summary>
public static class ApplicationModuleServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationModules(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddApplication();
        services.AddApplicationHandlers(static type =>
        {
            string? namespaceName = type.Namespace;
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return false;
            }

            return namespaceName.Contains(".Features.Countries.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.ParkFounders.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.ParkOperators.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.AttractionManufacturers.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.Parks.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.ParkZones.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.ParkItems.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.Images.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.Users.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.Search.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.DataSources.", StringComparison.Ordinal);
        });

        return services;
    }
}
