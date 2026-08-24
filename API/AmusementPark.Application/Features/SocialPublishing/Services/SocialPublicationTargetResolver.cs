using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.SocialPublishing.Services;

public sealed class SocialPublicationTargetResolver
{
    private readonly IPublicSeoContextProvider publicSeoContextProvider;
    private readonly ParkSocialPublicationTargetResolver parkTargetResolver;
    private readonly StandaloneAttractionSocialPublicationTargetResolver standaloneAttractionTargetResolver;
    private readonly ReferenceSocialPublicationTargetResolver referenceTargetResolver;
    private readonly ContentSocialPublicationTargetResolver contentTargetResolver;

    public SocialPublicationTargetResolver(
        IPublicSeoContextProvider publicSeoContextProvider,
        ParkSocialPublicationTargetResolver parkTargetResolver,
        StandaloneAttractionSocialPublicationTargetResolver standaloneAttractionTargetResolver,
        ReferenceSocialPublicationTargetResolver referenceTargetResolver,
        ContentSocialPublicationTargetResolver contentTargetResolver)
    {
        this.publicSeoContextProvider = publicSeoContextProvider;
        this.parkTargetResolver = parkTargetResolver;
        this.standaloneAttractionTargetResolver = standaloneAttractionTargetResolver;
        this.referenceTargetResolver = referenceTargetResolver;
        this.contentTargetResolver = contentTargetResolver;
    }

    internal async Task<ResolvedSocialPublicationTarget?> ResolveAsync(
        string? url,
        CancellationToken cancellationToken)
    {
        PublicSeoContext context = await this.publicSeoContextProvider.GetAsync(cancellationToken);
        Uri? normalizedUrl = NormalizePublicUrl(url, context);
        if (normalizedUrl is null)
        {
            return null;
        }

        string[] segments;
        try
        {
            segments = normalizedUrl.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.UnescapeDataString)
                .ToArray();
        }
        catch (UriFormatException)
        {
            return null;
        }

        if (segments.Length < 2
            || !context.SupportedLanguages.Contains(segments[0], StringComparer.OrdinalIgnoreCase)
            || IsPrivateRoute(segments[1]))
        {
            return null;
        }

        string route = segments[1].ToLowerInvariant();
        return route switch
        {
            "park" => await this.parkTargetResolver.ResolveAsync(normalizedUrl, segments, cancellationToken),
            "attraction" => await this.standaloneAttractionTargetResolver.ResolveAsync(normalizedUrl, segments, cancellationToken),
            "park-operator" or "park-founder" or "park-manufacturer" =>
                await this.referenceTargetResolver.ResolveAsync(normalizedUrl, segments, cancellationToken),
            "technical" or "rankings" =>
                await this.ResolveContentOrStaticPageAsync(normalizedUrl, segments, cancellationToken),
            _ => ResolveStaticPage(normalizedUrl, segments),
        };
    }

    private async Task<ResolvedSocialPublicationTarget?> ResolveContentOrStaticPageAsync(
        Uri normalizedUrl,
        IReadOnlyList<string> segments,
        CancellationToken cancellationToken)
    {
        if (segments.Count == 2)
        {
            return ResolveStaticPage(normalizedUrl, segments);
        }

        return await this.contentTargetResolver.ResolveAsync(normalizedUrl, segments, cancellationToken);
    }

    private static ResolvedSocialPublicationTarget? ResolveStaticPage(
        Uri normalizedUrl,
        IReadOnlyList<string> segments)
    {
        if (segments.Count != 2)
        {
            return null;
        }

        SocialPublicationPageNames? pageNames = ResolveStaticPageNames(segments[1]);
        return pageNames is null
            ? null
            : new ResolvedSocialPublicationTarget(
                normalizedUrl,
                SocialPublicationTargetKind.Page,
                pageNames.French,
                pageNames.English,
                null,
                null,
                null,
                null);
    }

    private static Uri? NormalizePublicUrl(string? value, PublicSeoContext context)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out Uri? candidate)
            || !Uri.TryCreate(context.PublicBaseUrl, UriKind.Absolute, out Uri? publicBaseUri)
            || !string.Equals(candidate.Scheme, publicBaseUri.Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(candidate.Host, publicBaseUri.Host, StringComparison.OrdinalIgnoreCase)
            || candidate.Port != publicBaseUri.Port
            || !string.IsNullOrWhiteSpace(candidate.UserInfo))
        {
            return null;
        }

        string normalizedQuery;
        try
        {
            normalizedQuery = RemoveQueryParameter(
                candidate.Query,
                SocialPublicationComposerService.FacebookImageQueryParameter);
        }
        catch (UriFormatException)
        {
            return null;
        }

        UriBuilder builder = new UriBuilder(candidate)
        {
            Fragment = string.Empty,
            Query = normalizedQuery,
        };
        return builder.Uri;
    }

    private static string RemoveQueryParameter(string query, string parameterName)
    {
        return string.Join(
            "&",
            query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Where(part => !string.Equals(
                    Uri.UnescapeDataString(part.Split('=', 2)[0]),
                    parameterName,
                    StringComparison.OrdinalIgnoreCase)));
    }

    private static SocialPublicationPageNames? ResolveStaticPageNames(string route)
    {
        return route.ToLowerInvariant() switch
        {
            "home" => new SocialPublicationPageNames("L’accueil d’Amusement Parks", "The Amusement Parks home page"),
            "parks" => new SocialPublicationPageNames("Les parcs d’attractions", "Amusement parks"),
            "sitemap" => new SocialPublicationPageNames("Le plan du site", "The site map"),
            "technical" => new SocialPublicationPageNames("Les guides techniques", "Technical guides"),
            "manufacturers" => new SocialPublicationPageNames("Les constructeurs d’attractions", "Attraction manufacturers"),
            "rankings" => new SocialPublicationPageNames("Les classements", "The rankings"),
            "about" => new SocialPublicationPageNames("À propos d’Amusement Parks", "About Amusement Parks"),
            "contact" => new SocialPublicationPageNames("Contacter Amusement Parks", "Contact Amusement Parks"),
            "versions" => new SocialPublicationPageNames("Les nouveautés d’Amusement Parks", "What’s new on Amusement Parks"),
            "privacy" => new SocialPublicationPageNames("La politique de confidentialité", "The privacy policy"),
            _ => null,
        };
    }

    private static bool IsPrivateRoute(string route)
    {
        return route.Equals("admin", StringComparison.OrdinalIgnoreCase)
            || route.Equals("profile", StringComparison.OrdinalIgnoreCase)
            || route.Equals("confirm-account", StringComparison.OrdinalIgnoreCase)
            || route.Equals("forgot-password", StringComparison.OrdinalIgnoreCase)
            || route.Equals("reset-password", StringComparison.OrdinalIgnoreCase)
            || route.Equals("not-found", StringComparison.OrdinalIgnoreCase);
    }
}

internal static class SocialPublicationLocalizedTextResolver
{
    public static string Resolve(
        IEnumerable<LocalizedText>? values,
        string languageCode,
        string fallback)
    {
        string? value = values?
            .FirstOrDefault(candidate => string.Equals(candidate.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))
            ?.Value;
        return string.IsNullOrWhiteSpace(value) ? fallback.Trim() : value.Trim();
    }
}

internal sealed record SocialPublicationPageNames(string French, string English);

internal sealed record ResolvedSocialPublicationTarget(
    Uri Url,
    SocialPublicationTargetKind Kind,
    string FrenchName,
    string EnglishName,
    ImageOwnerType? ImageOwnerType,
    string? ImageOwnerId,
    ImageCategory? ImageCategory,
    Park? Park)
{
    public string LanguageCode
    {
        get
        {
            return this.Url.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()
                ?.Trim()
                .ToLowerInvariant()
                ?? "fr";
        }
    }
}
