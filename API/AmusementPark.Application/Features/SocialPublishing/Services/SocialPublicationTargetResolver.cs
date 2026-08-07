using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.SocialPublishing.Services;

public sealed class SocialPublicationTargetResolver
{
    private readonly IPublicSeoContextProvider publicSeoContextProvider;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IVideoRepository videoRepository;

    public SocialPublicationTargetResolver(
        IPublicSeoContextProvider publicSeoContextProvider,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IVideoRepository videoRepository)
    {
        this.publicSeoContextProvider = publicSeoContextProvider;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.videoRepository = videoRepository;
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

        if (!string.Equals(segments[1], "park", StringComparison.OrdinalIgnoreCase))
        {
            PageNames? pageNames = ResolveStaticPageNames(segments);
            if (pageNames is null)
            {
                return null;
            }

            return new ResolvedSocialPublicationTarget(
                normalizedUrl,
                SocialPublicationTargetKind.Page,
                pageNames.French,
                pageNames.English,
                null,
                null,
                null);
        }

        if (segments.Length < 4 || string.IsNullOrWhiteSpace(segments[2]))
        {
            return null;
        }

        string parkId = segments[2];
        Park? park = await this.parkRepository.GetByIdAsync(parkId, false, cancellationToken);
        if (park is null || !park.IsPubliclyDiscoverable() || string.IsNullOrWhiteSpace(park.Name))
        {
            return null;
        }

        int itemSegmentIndex = segments.Length > 4
            && string.Equals(segments[4], "item", StringComparison.OrdinalIgnoreCase)
                ? 4
                : -1;
        ParkItem? item = await this.ResolveParkItemAsync(segments, itemSegmentIndex, parkId, cancellationToken);
        if (itemSegmentIndex >= 0 && item is null)
        {
            return null;
        }

        int entityBaseLength = item is null ? 4 : itemSegmentIndex + 3;
        bool isVideoDetailRoute = segments.Length == entityBaseLength + 3
            && string.Equals(segments[entityBaseLength], "videos", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(segments[entityBaseLength + 1])
            && !string.IsNullOrWhiteSpace(segments[entityBaseLength + 2]);
        if (isVideoDetailRoute)
        {
            return await this.ResolveVideoTargetAsync(
                normalizedUrl,
                segments,
                entityBaseLength,
                parkId,
                item,
                cancellationToken);
        }

        ImageOwnerType ownerType = item is null ? ImageOwnerType.Park : ImageOwnerType.ParkItem;
        string ownerId = item?.Id ?? parkId;
        ImageCategory category = item is null ? ImageCategory.Park : ImageCategory.ParkItem;
        if (segments.Length == entityBaseLength)
        {
            return new ResolvedSocialPublicationTarget(
                normalizedUrl,
                item is null ? SocialPublicationTargetKind.Park : SocialPublicationTargetKind.ParkItem,
                item?.Name ?? park.Name,
                item?.Name ?? park.Name,
                ownerType,
                ownerId,
                category);
        }

        PageNames? names = ResolveParkPageNames(segments, entityBaseLength, item?.Name ?? park.Name, item is not null);
        if (names is null)
        {
            return null;
        }

        return new ResolvedSocialPublicationTarget(
            normalizedUrl,
            SocialPublicationTargetKind.Page,
            names.French,
            names.English,
            ownerType,
            ownerId,
            category);
    }

    private async Task<ParkItem?> ResolveParkItemAsync(
        IReadOnlyList<string> segments,
        int itemSegmentIndex,
        string parkId,
        CancellationToken cancellationToken)
    {
        if (itemSegmentIndex < 0)
        {
            return null;
        }

        if (itemSegmentIndex + 2 >= segments.Count)
        {
            return null;
        }

        ParkItem? item = await this.parkItemRepository.GetByIdAsync(
            segments[itemSegmentIndex + 1],
            false,
            cancellationToken);
        return item is not null
            && !string.IsNullOrWhiteSpace(item.Id)
            && item.IsVisible
            && string.Equals(item.ParkId, parkId, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(item.Name)
                ? item
                : null;
    }

    private async Task<ResolvedSocialPublicationTarget?> ResolveVideoTargetAsync(
        Uri normalizedUrl,
        IReadOnlyList<string> segments,
        int videosSegmentIndex,
        string parkId,
        ParkItem? item,
        CancellationToken cancellationToken)
    {
        Video? video = await this.videoRepository.GetByIdAsync(segments[videosSegmentIndex + 1], cancellationToken);
        if (video is null || !video.IsPublished || string.IsNullOrWhiteSpace(video.Title))
        {
            return null;
        }

        if (item is not null
            && (video.OwnerType != VideoOwnerType.ParkItem
                || !string.Equals(video.OwnerId, item.Id, StringComparison.Ordinal)))
        {
            return null;
        }

        if (item is null
            && (video.OwnerType != VideoOwnerType.Park
                || !string.Equals(video.OwnerId, parkId, StringComparison.Ordinal)))
        {
            return null;
        }

        return new ResolvedSocialPublicationTarget(
            normalizedUrl,
            SocialPublicationTargetKind.Video,
            ResolveLocalizedText(video.Titles, "fr", video.Title),
            ResolveLocalizedText(video.Titles, "en", video.Title),
            item is null ? ImageOwnerType.Park : ImageOwnerType.ParkItem,
            item?.Id ?? parkId,
            item is null ? ImageCategory.Park : ImageCategory.ParkItem);
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

    private static PageNames? ResolveParkPageNames(
        IReadOnlyList<string> segments,
        int entityBaseLength,
        string entityName,
        bool isParkItem)
    {
        int suffixLength = segments.Count - entityBaseLength;
        if (suffixLength == 3
            && string.Equals(segments[entityBaseLength], "history", StringComparison.OrdinalIgnoreCase)
            && string.Equals(segments[entityBaseLength + 1], "page", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(segments[entityBaseLength + 2], out int page)
            && page > 0)
        {
            return new PageNames($"L’histoire de {entityName}", $"The history of {entityName}");
        }

        if (suffixLength != 1)
        {
            return null;
        }

        string section = segments[entityBaseLength].ToLowerInvariant();
        return section switch
        {
            "images" => new PageNames($"Les photos de {entityName}", $"{entityName} photos"),
            "history" => new PageNames($"L’histoire de {entityName}", $"The history of {entityName}"),
            "videos" => new PageNames($"Les vidéos de {entityName}", $"{entityName} videos"),
            "comments" => new PageNames($"Les avis sur {entityName}", $"Reviews of {entityName}"),
            "map" when !isParkItem => new PageNames($"La carte de {entityName}", $"The map of {entityName}"),
            "zones" when !isParkItem => new PageNames($"Les zones de {entityName}", $"Areas at {entityName}"),
            "weather" when !isParkItem => new PageNames($"La météo de {entityName}", $"The weather at {entityName}"),
            "opening-hours" when !isParkItem => new PageNames($"Les horaires de {entityName}", $"Opening hours for {entityName}"),
            "items" when !isParkItem => new PageNames($"Les attractions et lieux de {entityName}", $"Attractions and places at {entityName}"),
            _ => null,
        };
    }

    private static PageNames? ResolveStaticPageNames(IReadOnlyList<string> segments)
    {
        if (segments.Count != 2)
        {
            return null;
        }

        string route = segments.Count > 1 ? segments[1].ToLowerInvariant() : string.Empty;
        return route switch
        {
            "home" => new PageNames("L’accueil d’Amusement Parks", "The Amusement Parks home page"),
            "parks" => new PageNames("Les parcs d’attractions", "Amusement parks"),
            "sitemap" => new PageNames("Le plan du site", "The site map"),
            "technical" => new PageNames("Les guides techniques", "Technical guides"),
            "manufacturers" => new PageNames("Les constructeurs d’attractions", "Attraction manufacturers"),
            "rankings" => new PageNames("Les classements", "The rankings"),
            "about" => new PageNames("À propos d’Amusement Parks", "About Amusement Parks"),
            "contact" => new PageNames("Contacter Amusement Parks", "Contact Amusement Parks"),
            "versions" => new PageNames("Les nouveautés d’Amusement Parks", "What’s new on Amusement Parks"),
            "privacy" => new PageNames("La politique de confidentialité", "The privacy policy"),
            _ => null,
        };
    }

    private static string ResolveLocalizedText(
        IEnumerable<LocalizedText> values,
        string languageCode,
        string fallback)
    {
        string? value = values
            .FirstOrDefault(candidate => string.Equals(candidate.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))
            ?.Value;
        return string.IsNullOrWhiteSpace(value) ? fallback.Trim() : value.Trim();
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

    private sealed record PageNames(string French, string English);
}

internal sealed record ResolvedSocialPublicationTarget(
    Uri Url,
    SocialPublicationTargetKind Kind,
    string FrenchName,
    string EnglishName,
    ImageOwnerType? ImageOwnerType,
    string? ImageOwnerId,
    ImageCategory? ImageCategory)
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
