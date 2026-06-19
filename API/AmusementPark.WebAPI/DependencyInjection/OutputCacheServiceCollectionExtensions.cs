using System;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;

namespace AmusementPark.WebAPI.DependencyInjection;

/// <summary>
/// Configure le cache de sortie des endpoints publics stables.
/// </summary>
public static class OutputCacheServiceCollectionExtensions
{
    public static IServiceCollection AddApiOutputCaching(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOutputCache(options =>
        {
            options.AddPolicy(ApiOutputCachePolicyNames.PublicSeoDocuments, policy => policy
                .With(IsAnonymousCacheCandidate)
                .Cache()
                .Expire(TimeSpan.FromHours(6))
                .SetVaryByHeader("Host", "X-Forwarded-Host", "X-Forwarded-Proto")
                .SetVaryByQuery("*")
                .Tag(ApiOutputCachePolicyNames.PublicSeoTag));

            options.AddPolicy(ApiOutputCachePolicyNames.PublicDataShort, policy => policy
                .With(IsAnonymousCacheCandidate)
                .Cache()
                .Expire(TimeSpan.FromMinutes(5))
                .SetVaryByHeader("Host", "X-Forwarded-Host", "X-Forwarded-Proto", "Accept-Language")
                .SetVaryByQuery("*")
                .Tag(ApiOutputCachePolicyNames.PublicDataTag));

            options.AddPolicy(ApiOutputCachePolicyNames.PublicDataMedium, policy => policy
                .With(IsAnonymousCacheCandidate)
                .Cache()
                .Expire(TimeSpan.FromMinutes(30))
                .SetVaryByHeader("Host", "X-Forwarded-Host", "X-Forwarded-Proto", "Accept-Language")
                .SetVaryByQuery("*")
                .Tag(ApiOutputCachePolicyNames.PublicDataTag));

            options.AddPolicy(ApiOutputCachePolicyNames.PublicWeatherDataShort, policy => policy
                .With(IsAnonymousCacheCandidate)
                .Cache()
                .Expire(TimeSpan.FromMinutes(5))
                .SetVaryByHeader("Host", "X-Forwarded-Host", "X-Forwarded-Proto", "Accept-Language")
                .SetVaryByQuery("*")
                .Tag(ApiOutputCachePolicyNames.PublicWeatherDataTag));

            options.AddPolicy(ApiOutputCachePolicyNames.PublicReferenceData, policy => policy
                .With(IsAnonymousCacheCandidate)
                .Cache()
                .Expire(TimeSpan.FromHours(6))
                .SetVaryByHeader("Host", "X-Forwarded-Host", "X-Forwarded-Proto", "Accept-Language")
                .SetVaryByQuery("*")
                .Tag(ApiOutputCachePolicyNames.PublicReferenceDataTag));
        });

        return services;
    }

    private static bool IsAnonymousCacheCandidate(OutputCacheContext context)
    {
        // Les cookies analytiques (Matomo, consentement, etc.) ne doivent pas casser le cache.
        // Les endpoints publics utilisent des DTO publics et les appels SSR/front publics
        // sont faits sans Authorization via anonymousHttpOptions().
        if (context.HttpContext.Request.Headers.ContainsKey("Authorization"))
        {
            return false;
        }

        return context.HttpContext.User?.Identity?.IsAuthenticated != true;
    }
}
