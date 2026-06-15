using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace AmusementPark.WebAPI.OutputCaching;

/// <summary>
/// Ajoute des headers HTTP cache côté navigateur/proxy pour les GET publics anonymes.
/// Ce middleware s'exécute aussi lorsque OutputCache sert une réponse déjà mise en cache.
/// </summary>
public sealed class PublicHttpCacheHeadersMiddleware
{
    // Le stale-while-revalidate (SWR) est retiré des endpoints de contenu éditable :
    // il faisait servir au navigateur une copie périmée juste après une mise à jour
    // admin (« flicker » frais -> périmé). Il reste actif pour les ressources stables
    // par URL (images) et les documents SEO (régénérés et évincés explicitement).
    private static readonly IReadOnlyList<PublicHttpCacheRule> Rules = new[]
    {
        new PublicHttpCacheRule("/robots.txt", 600, 3600, 3600, false),
        new PublicHttpCacheRule("/sitemap.xml", 600, 3600, 3600, false),
        new PublicHttpCacheRule("/sitemaps", 600, 3600, 3600, false),
        new PublicHttpCacheRule("/indexnow", 600, 3600, 3600, false),
        new PublicHttpCacheRule("/public-stats/home", 60, 300, 0, true),
        new PublicHttpCacheRule("/parks/random-visible", 60, 300, 0, true),
        new PublicHttpCacheRule("/parks/home-featured", 60, 300, 0, true),
        new PublicHttpCacheRule("/parks/map-visible", 120, 600, 0, true),
        new PublicHttpCacheRule("/parks", 60, 300, 0, true),
        new PublicHttpCacheRule("/park-zones", 120, 600, 0, true),
        new PublicHttpCacheRule("/park-items", 120, 600, 0, true),
        new PublicHttpCacheRule("/images", 300, 900, 900, true),
        new PublicHttpCacheRule("/countries", 3600, 21600, 0, true),
        new PublicHttpCacheRule("/search", 60, 300, 0, true),
        new PublicHttpCacheRule("/attraction-manufacturers", 3600, 21600, 0, true),
        new PublicHttpCacheRule("/park-founders", 3600, 21600, 0, true),
        new PublicHttpCacheRule("/park-operators", 3600, 21600, 0, true),
    };

    private readonly RequestDelegate next;

    public PublicHttpCacheHeadersMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (CanRegisterCacheHeaders(context))
        {
            context.Response.OnStarting(static state =>
            {
                HttpContext httpContext = (HttpContext)state;
                ApplyCacheHeaders(httpContext);
                return Task.CompletedTask;
            }, context);
        }

        await this.next(context);
    }

    private static bool CanRegisterCacheHeaders(HttpContext context)
    {
        return HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method);
    }

    private static void ApplyCacheHeaders(HttpContext context)
    {
        if (context.Response.StatusCode != StatusCodes.Status200OK)
        {
            return;
        }

        if (context.Request.Headers.ContainsKey(HeaderNames.Authorization))
        {
            return;
        }

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            return;
        }

        if (!StringValues.IsNullOrEmpty(context.Response.Headers.SetCookie))
        {
            return;
        }

        if (!TryResolveRule(context.Request.Path, out PublicHttpCacheRule rule))
        {
            return;
        }

        string existingCacheControl = context.Response.Headers.CacheControl.ToString();
        if (ContainsDirective(existingCacheControl, "no-store") || ContainsDirective(existingCacheControl, "private"))
        {
            return;
        }

        context.Response.Headers.CacheControl = BuildCacheControlValue(rule);
        context.Response.Headers.Remove(HeaderNames.Pragma);
        context.Response.Headers.Remove(HeaderNames.Expires);

        if (rule.VaryByAcceptLanguage)
        {
            AppendVaryHeader(context.Response.Headers, HeaderNames.AcceptLanguage);
        }
    }

    private static bool TryResolveRule(PathString path, out PublicHttpCacheRule rule)
    {
        foreach (PublicHttpCacheRule candidate in Rules)
        {
            if (path.StartsWithSegments(candidate.PathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                rule = candidate;
                return true;
            }
        }

        rule = default;
        return false;
    }

    private static string BuildCacheControlValue(PublicHttpCacheRule rule)
    {
        List<string> directives = new()
        {
            "public",
            $"max-age={rule.MaxAgeSeconds}"
        };

        if (rule.SharedMaxAgeSeconds > 0)
        {
            directives.Add($"s-maxage={rule.SharedMaxAgeSeconds}");
        }

        if (rule.StaleWhileRevalidateSeconds > 0)
        {
            directives.Add($"stale-while-revalidate={rule.StaleWhileRevalidateSeconds}");
        }

        return string.Join(", ", directives);
    }

    private static bool ContainsDirective(string value, string directive)
    {
        return value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(part => string.Equals(part, directive, StringComparison.OrdinalIgnoreCase));
    }

    private static void AppendVaryHeader(IHeaderDictionary headers, string headerName)
    {
        string[] existingValues = headers.Vary.ToString()
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (existingValues.Any(value => string.Equals(value, headerName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        headers.Vary = existingValues.Length == 0
            ? headerName
            : string.Join(", ", existingValues.Concat(new[] { headerName }));
    }

    private readonly record struct PublicHttpCacheRule(
        string PathPrefix,
        int MaxAgeSeconds,
        int SharedMaxAgeSeconds,
        int StaleWhileRevalidateSeconds,
        bool VaryByAcceptLanguage);
}
