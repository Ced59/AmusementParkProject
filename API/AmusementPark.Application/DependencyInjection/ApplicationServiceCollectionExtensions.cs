using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.BackgroundJobs.Services;
using AmusementPark.Application.Features.Contact.Ports;
using AmusementPark.Application.Features.Contact.Services;
using AmusementPark.Application.Features.Comments.Services;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.Countries.Services;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.ParkItems.Services;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.ParkWeather.Services;
using AmusementPark.Application.Features.Parks.Services;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Services;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;
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
        services.AddScoped<IDurableBackgroundJobHandlerResolver, DurableBackgroundJobHandlerRegistry>();
        services.AddSingleton<DurableBackgroundJobRetryDelayCalculator>();
        services.AddScoped<DurableBackgroundJobExecutionOrchestrator>();
        services.AddScoped<ParkItemReferenceValidator>();
        services.AddScoped<CommentTargetResolver>();
        services.AddScoped<CommentImageManager>();
        services.AddScoped<CommentImageReconciler>();
        services.AddScoped<ParkItemsBulkCreatePreviewService>();
        services.AddScoped<BulkParkGraphJsonExportDataLoader>();
        services.AddScoped<ParkGraphUpsertProcessor>();
        services.AddScoped<BulkParkGraphUpsertProcessor>();
        services.AddScoped<ISocialPublicationService, SocialPublicationService>();
        services.AddScoped<SocialPublicationReconciler>();
        services.AddScoped<ParkSocialPublicationTargetResolver>();
        services.AddScoped<StandaloneAttractionSocialPublicationTargetResolver>();
        services.AddScoped<ReferenceSocialPublicationTargetResolver>();
        services.AddScoped<ContentSocialPublicationTargetResolver>();
        services.AddScoped<SocialPublicationTargetResolver>();
        services.AddScoped<ISocialPublicationComposerService, SocialPublicationComposerService>();
        services.AddScoped<ParkOpeningHoursScheduleNormalizer>();
        services.AddSingleton<ParkOpeningHoursCoverageSegmentBuilder>();
        services.AddSingleton<ParkOpeningHoursAdminStatusResolver>();
        services.AddScoped<ParkOpeningHoursCoverageNotificationProcessor>();
        services.AddSingleton<ParkOpeningHoursCalendarBuilder>();
        services.AddScoped<ParkWeatherRefreshStarter>();
        services.AddScoped<ParkWeatherRefreshOrchestrator>();
        services.AddSingleton<ParkWeatherLocalDateResolver>();
        services.AddSingleton<ParkWeatherHistoricalComparisonDateResolver>();
        services.AddScoped<IContactNotificationService, NoOpContactNotificationService>();
        services.AddScoped<IParkWeatherNotificationService, NoOpParkWeatherNotificationService>();
        services.AddScoped<IParkOpeningHoursNotificationService, NoOpParkOpeningHoursNotificationService>();
        services.AddSingleton<IMeasurementConversionService>(MeasurementConversionService.Instance);
        services.AddSingleton<IRankingScopeRegistry>(
            new RankingScopeRegistry(CanonicalRankingScopes.Version, CanonicalRankingScopes.All));
        services.AddScoped<IRatingRankProvider, RatingRankProvider>();
        services.AddScoped<UserRankingShareAccessResolver>();
        services.AddScoped<ICountryReferenceService, CountryReferenceService>();
        services.AddScoped<ISitemapSectionProvider, StaticPagesSitemapSectionProvider>();
        services.AddScoped<ISitemapSectionProvider, ParksSitemapSectionProvider>();
        services.AddScoped<ISitemapSectionProvider, ParkOpeningHoursSitemapSectionProvider>();
        services.AddScoped<ISitemapSectionProvider, ParkPricingSitemapSectionProvider>();
        services.AddScoped<ISitemapSectionProvider, HistoryTimelinesSitemapSectionProvider>();
        services.AddScoped<ISitemapSectionProvider, HistoryArticlesSitemapSectionProvider>();
        services.AddScoped<ISitemapSectionProvider, ParkImagesSitemapSectionProvider>();
        services.AddScoped<ISitemapSectionProvider, ParkVideosSitemapSectionProvider>();
        services.AddScoped<ISitemapSectionProvider, ParkItemListsSitemapSectionProvider>();
        services.AddScoped<ISitemapSectionProvider, ParkZonesSitemapSectionProvider>();
        services.AddScoped<ISitemapSectionProvider, ParkItemsSitemapSectionProvider>();
        services.AddScoped<ISitemapSectionProvider, StandaloneAttractionsSitemapSectionProvider>();
        services.AddScoped<ISitemapSectionProvider, ParkItemImagesSitemapSectionProvider>();
        services.AddScoped<ISitemapSectionProvider, ParkItemVideosSitemapSectionProvider>();
        services.AddScoped<ISitemapSectionProvider, ReferencesSitemapSectionProvider>();
        services.AddScoped<ISitemapSectionProvider, TechnicalPagesSitemapSectionProvider>();
        services.AddScoped<ISitemapXmlWriter, SitemapXmlWriter>();
        services.AddScoped<SeoSitemapGenerationOrchestrator>();
        services.AddScoped<PublicSeoUrlResolver>();
        services.AddScoped<IPublicSeoUpdateNotifier, PublicSeoUpdateNotifier>();
        services.AddSingleton<ISeoSitemapRefreshScheduler, NoOpSeoSitemapRefreshScheduler>();
        services.AddSingleton<ISeoSitemapRuntimeStateStore, InMemorySeoSitemapRuntimeStateStore>();
        return services;
    }

    public static IServiceCollection AddDurableBackgroundJobHandler<THandler>(this IServiceCollection services)
        where THandler : class, IDurableBackgroundJobHandler
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IDurableBackgroundJobHandler, THandler>();
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
