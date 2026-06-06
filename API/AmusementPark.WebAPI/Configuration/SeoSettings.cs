using System.Net;

namespace AmusementPark.WebAPI.Configuration;

/// <summary>
/// Paramètres SEO publics utilisés pour robots.txt et sitemap.xml.
/// </summary>
public sealed class SeoSettings
{
    private const string DefaultPublicBaseUrl = "https://amusement-parks.fun";

    public const string SectionName = "Seo";

    public string PublicBaseUrl { get; set; } = DefaultPublicBaseUrl;

    public string DefaultLanguage { get; set; } = "en";

    public List<string> SupportedLanguages { get; set; } = new()
    {
        "en",
        "fr",
        "es",
        "de",
        "it",
        "pl",
        "nl",
        "pt",
    };

    public int MaxDynamicUrlsPerType { get; set; } = 5000;

    public List<string> RobotsDisallowPaths { get; set; } = new()
    {
        "/api/",
        "/{lang}/admin/",
        "/{lang}/profile",
        "/{lang}/confirm-account",
        "/{lang}/forgot-password",
        "/{lang}/reset-password",
    };

    public string GetNormalizedPublicBaseUrl(bool requireHttps)
    {
        string value = string.IsNullOrWhiteSpace(this.PublicBaseUrl)
            ? DefaultPublicBaseUrl
            : this.PublicBaseUrl.Trim();

        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) || !IsHttpScheme(uri))
        {
            if (requireHttps)
            {
                throw new InvalidOperationException("Seo:PublicBaseUrl must be an absolute HTTP(S) root origin in production.");
            }

            return DefaultPublicBaseUrl;
        }

        if (requireHttps && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Seo:PublicBaseUrl must use HTTPS in production.");
        }

        if (requireHttps && IsLocalHost(uri.Host))
        {
            throw new InvalidOperationException("Seo:PublicBaseUrl cannot target localhost in production.");
        }

        if (requireHttps && !HasRootOnlyPath(uri))
        {
            throw new InvalidOperationException("Seo:PublicBaseUrl must be a root origin without path, query or fragment in production.");
        }

        return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }

    private static bool IsHttpScheme(Uri uri)
    {
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    private static bool IsLocalHost(string host)
    {
        string normalizedHost = host.Trim();

        if (normalizedHost.StartsWith("[", StringComparison.Ordinal) &&
            normalizedHost.EndsWith("]", StringComparison.Ordinal) &&
            normalizedHost.Length > 2)
        {
            normalizedHost = normalizedHost[1..^1];
        }

        if (string.Equals(normalizedHost, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(normalizedHost, out IPAddress? address) &&
               IPAddress.IsLoopback(address);
    }

    private static bool HasRootOnlyPath(Uri uri)
    {
        bool hasRootPath = string.IsNullOrEmpty(uri.AbsolutePath) ||
                           string.Equals(uri.AbsolutePath, "/", StringComparison.Ordinal);

        return hasRootPath && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
    }
}
