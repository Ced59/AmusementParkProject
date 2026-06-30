using System;
using AmusementPark.Application.DependencyInjection;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.WebAPI.Configuration;
using AmusementPark.WebAPI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AmusementPark.WebAPI.DependencyInjection;

/// <summary>
/// Regroupe l'enregistrement des handlers applicatifs activés pour l'API.
/// </summary>
public static class ApplicationModuleServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationModules(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<SeoSettings>(configuration.GetSection(SeoSettings.SectionName));
        services.AddScoped<IPublicSeoContextProvider, SeoPublicContextProvider>();
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
                   namespaceName.Contains(".Features.AttractionAccessConditionTypes.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.ContextualBlocks.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.Parks.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.ParkZones.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.ParkItems.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.ParkGraphUpserts.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.ParkOpeningHours.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.ParkWeather.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.Images.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.History.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.Videos.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.Contact.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.LocalizedContent.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.Users.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.Search.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.DataSources.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.AdminAudit.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.TechnicalPages.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.TechnicalStats.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.SocialShare.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.Ratings.", StringComparison.Ordinal) ||
                   namespaceName.Contains(".Features.Seo.", StringComparison.Ordinal);
        });

        return services;
    }
}
