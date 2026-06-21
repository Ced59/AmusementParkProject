using System;
using System.Collections.Generic;
using System.Linq;
using AmusementPark.WebAPI.AdminPublicView;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace AmusementPark.WebAPI.OutputCaching;

internal static class PublicHttpCacheHeadersApplicator
{
    // Stale-while-revalidate stays disabled for editable content endpoints because
    // it can briefly serve stale data right after an admin update. It remains
    // enabled for URL-stable resources such as images and generated SEO documents.
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
        new PublicHttpCacheRule("/videos", 300, 900, 0, true),
        new PublicHttpCacheRule("/countries", 3600, 21600, 0, true),
        new PublicHttpCacheRule("/search", 60, 300, 0, true),
        new PublicHttpCacheRule("/attraction-manufacturers", 3600, 21600, 0, true),
        new PublicHttpCacheRule("/park-founders", 3600, 21600, 0, true),
        new PublicHttpCacheRule("/park-operators", 3600, 21600, 0, true),
    };

    public static void Apply(HttpContext context)
    {
        if (context.Response.StatusCode != StatusCodes.Status200OK)
        {
            return;
        }

        if (AdminPublicViewSimulation.HasRequestHeader(context.Request.Headers))
        {
            AdminPublicViewSimulation.ApplyNoStoreHeaders(context);
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
        if (ShouldKeepExistingCacheControl(existingCacheControl, rule))
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

    private static bool ShouldKeepExistingCacheControl(string value, PublicHttpCacheRule rule)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (ContainsDirective(value, "no-store") || ContainsDirective(value, "private") || ContainsDirective(value, "immutable"))
        {
            return true;
        }

        int? existingMaxAgeSeconds = TryReadSecondsDirective(value, "max-age");
        return existingMaxAgeSeconds.HasValue && existingMaxAgeSeconds.Value >= rule.MaxAgeSeconds;
    }

    private static int? TryReadSecondsDirective(string value, string directive)
    {
        foreach (string part in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            int separatorIndex = part.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                continue;
            }

            string name = part[..separatorIndex];
            if (!string.Equals(name, directive, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string rawSeconds = part[(separatorIndex + 1)..];
            if (int.TryParse(rawSeconds, out int seconds))
            {
                return seconds;
            }

            return null;
        }

        return null;
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
