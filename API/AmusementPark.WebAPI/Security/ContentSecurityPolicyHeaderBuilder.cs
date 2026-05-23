using System;
using System.Collections.Generic;
using System.Linq;
using AmusementPark.WebAPI.Configuration;

namespace AmusementPark.WebAPI.Security;

/// <summary>
/// Builds a normalized CSP header value from configuration.
/// </summary>
public static class ContentSecurityPolicyHeaderBuilder
{
    private static readonly string[] PreferredDirectiveOrder =
    [
        "default-src",
        "base-uri",
        "object-src",
        "frame-ancestors",
        "form-action",
        "script-src",
        "style-src",
        "font-src",
        "img-src",
        "connect-src",
        "frame-src",
        "worker-src",
        "media-src",
        "manifest-src",
        "report-uri"
    ];

    public static string Build(ContentSecurityPolicySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!string.IsNullOrWhiteSpace(settings.Policy))
        {
            return NormalizePolicy(settings.Policy);
        }

        Dictionary<string, string[]> directives = settings.Directives ?? new Dictionary<string, string[]>();
        List<string> renderedDirectives = new List<string>();

        foreach (string directiveName in PreferredDirectiveOrder)
        {
            AddDirectiveIfConfigured(renderedDirectives, directives, directiveName);
        }

        IEnumerable<KeyValuePair<string, string[]>> remainingDirectives = directives
            .Where(directive => !PreferredDirectiveOrder.Contains(directive.Key, StringComparer.OrdinalIgnoreCase))
            .OrderBy(directive => directive.Key, StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, string[]> directive in remainingDirectives)
        {
            AddDirective(renderedDirectives, directive.Key, directive.Value);
        }

        if (!string.IsNullOrWhiteSpace(settings.ReportUri)
            && !renderedDirectives.Any(static directive => directive.StartsWith("report-uri ", StringComparison.OrdinalIgnoreCase)))
        {
            renderedDirectives.Add($"report-uri {settings.ReportUri.Trim()}");
        }

        return string.Join("; ", renderedDirectives);
    }

    private static void AddDirectiveIfConfigured(
        ICollection<string> renderedDirectives,
        IReadOnlyDictionary<string, string[]> directives,
        string directiveName)
    {
        if (!directives.TryGetValue(directiveName, out string[]? sources))
        {
            return;
        }

        AddDirective(renderedDirectives, directiveName, sources);
    }

    private static void AddDirective(
        ICollection<string> renderedDirectives,
        string directiveName,
        IEnumerable<string>? sources)
    {
        if (string.IsNullOrWhiteSpace(directiveName))
        {
            return;
        }

        string[] normalizedSources = sources?
            .Where(static source => !string.IsNullOrWhiteSpace(source))
            .Select(static source => source.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        if (normalizedSources.Length == 0)
        {
            renderedDirectives.Add(directiveName.Trim());
            return;
        }

        renderedDirectives.Add($"{directiveName.Trim()} {string.Join(' ', normalizedSources)}");
    }

    private static string NormalizePolicy(string policy)
    {
        string[] directives = policy
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static directive => !string.IsNullOrWhiteSpace(directive))
            .ToArray();

        return string.Join("; ", directives);
    }
}
