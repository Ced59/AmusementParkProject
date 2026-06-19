using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Parks;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;

namespace AmusementPark.WebAPI.OutputCaching;

public sealed class ParkWeatherPublicCacheInvalidator : IParkWeatherCacheInvalidator
{
    private static readonly IReadOnlyCollection<string> PublicLanguages = new[]
    {
        "fr",
        "en",
        "de",
        "nl",
        "pl",
        "pt",
        "it",
        "es",
    };

    private readonly IOutputCacheStore outputCacheStore;
    private readonly ISsrPageCacheInvalidator ssrPageCacheInvalidator;
    private readonly ILogger<ParkWeatherPublicCacheInvalidator> logger;

    public ParkWeatherPublicCacheInvalidator(
        IOutputCacheStore outputCacheStore,
        ISsrPageCacheInvalidator ssrPageCacheInvalidator,
        ILogger<ParkWeatherPublicCacheInvalidator> logger)
    {
        this.outputCacheStore = outputCacheStore;
        this.ssrPageCacheInvalidator = ssrPageCacheInvalidator;
        this.logger = logger;
    }

    public async Task InvalidateUpdatedWeatherAsync(IReadOnlyCollection<Park> parks, CancellationToken cancellationToken)
    {
        if (parks.Count == 0)
        {
            return;
        }

        try
        {
            await this.outputCacheStore.EvictByTagAsync(ApiOutputCachePolicyNames.PublicWeatherDataTag, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            this.logger.LogWarning(exception, "Weather API output cache eviction failed.");
        }

        SsrPageCacheInvalidationRequest request = BuildSsrInvalidationRequest(parks);
        if (request.Paths.Count == 0)
        {
            return;
        }

        try
        {
            await this.ssrPageCacheInvalidator.InvalidateAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            this.logger.LogWarning(exception, "Weather SSR page cache invalidation failed.");
        }
    }

    private static SsrPageCacheInvalidationRequest BuildSsrInvalidationRequest(IReadOnlyCollection<Park> parks)
    {
        HashSet<string> paths = new HashSet<string>(StringComparer.Ordinal);

        foreach (Park park in parks)
        {
            if (string.IsNullOrWhiteSpace(park.Id))
            {
                continue;
            }

            foreach (string language in PublicLanguages)
            {
                string basePath = BuildParkBasePath(language, park);
                paths.Add(basePath);
                paths.Add($"{basePath}/weather");
            }
        }

        return new SsrPageCacheInvalidationRequest
        {
            All = false,
            Paths = paths.ToList(),
            Prefixes = Array.Empty<string>(),
            IncludeSeoDocuments = false,
            AllowStale = false,
            Refresh = false,
        };
    }

    private static string BuildParkBasePath(string language, Park park)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        return $"/{language}/park/{park.Id}/{parkSlug}";
    }
}
