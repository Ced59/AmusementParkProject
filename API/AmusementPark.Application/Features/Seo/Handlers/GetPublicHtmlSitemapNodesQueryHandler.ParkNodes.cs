using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.TechnicalPages;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Seo.Handlers;

public sealed partial class GetPublicHtmlSitemapNodesQueryHandler
{
    private async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildParkNodesAsync(
        string language,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Park> parks = await SitemapPublicCandidateLoader.LoadPublicParksAsync(this.parkRepository, cancellationToken);
        return parks
            .OrderBy(static park => park.Name, StringComparer.OrdinalIgnoreCase)
            .Select(park => new PublicHtmlSitemapNode
            {
                Id = $"park:{park.Id}",
                Label = park.Name ?? park.Id ?? Label(language, "park"),
                RelativeUrl = BuildParkPath(language, park),
                HasChildren = true,
            })
            .ToList();
    }

    private async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildParkChildNodesAsync(
        string language,
        string parkId,
        CancellationToken cancellationToken)
    {
        Park? park = await this.GetPublicParkAsync(parkId, cancellationToken);
        if (park is null)
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        string parkPath = BuildParkPath(language, park);
        IReadOnlyCollection<ParkItem> publicItems = await this.GetPublicParkItemsAsync(park.Id!, cancellationToken);
        List<PublicHtmlSitemapNode> nodes = new List<PublicHtmlSitemapNode>();

        if (publicItems.Any(ParksSitemapSectionProvider.HasPublicMapMarker))
        {
            nodes.Add(CreateLeaf($"park-map:{park.Id}", Label(language, "interactiveMap"), $"{parkPath}/map"));
        }

        if (HasPosition(park))
        {
            nodes.Add(CreateLeaf($"park-weather:{park.Id}", Label(language, "weather"), $"{parkPath}/weather"));
        }

        if (await this.HasOpeningHoursAsync(park.Id!, cancellationToken))
        {
            nodes.Add(CreateLeaf($"park-opening-hours:{park.Id}", Label(language, "openingHours"), $"{parkPath}/opening-hours"));
        }

        if (await this.HasParkImagesAsync(park.Id!, publicItems, cancellationToken))
        {
            nodes.Add(CreateLeaf($"park-images:{park.Id}", Label(language, "images"), $"{parkPath}/images"));
        }

        if (await this.HasVideosAsync(VideoOwnerType.Park, park.Id!, language, cancellationToken))
        {
            nodes.Add(new PublicHtmlSitemapNode
            {
                Id = $"park-videos:{park.Id}",
                Label = Label(language, "videos"),
                RelativeUrl = $"{parkPath}/videos",
                HasChildren = true,
            });
        }

        if (await this.HasVisibleZonesAsync(park.Id!, publicItems, cancellationToken))
        {
            nodes.Add(new PublicHtmlSitemapNode
            {
                Id = $"park-zones:{park.Id}",
                Label = Label(language, "zones"),
                RelativeUrl = $"{parkPath}/zones",
                HasChildren = true,
            });
        }

        if (publicItems.Count > 0)
        {
            nodes.Add(new PublicHtmlSitemapNode
            {
                Id = $"park-items:{park.Id}",
                Label = Label(language, "items"),
                RelativeUrl = $"{parkPath}/items",
                HasChildren = true,
            });
        }

        if (await this.HasParkHistoryAsync(park, publicItems, cancellationToken))
        {
            nodes.Add(new PublicHtmlSitemapNode
            {
                Id = $"park-history:{park.Id}",
                Label = Label(language, "history"),
                RelativeUrl = $"{parkPath}/history",
                HasChildren = true,
            });
        }

        return nodes;
    }

    private async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildParkItemNodesAsync(
        string language,
        string parkId,
        CancellationToken cancellationToken)
    {
        Park? park = await this.GetPublicParkAsync(parkId, cancellationToken);
        if (park is null)
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        string parkPath = BuildParkPath(language, park);
        IReadOnlyCollection<ParkItem> items = await this.GetPublicParkItemsAsync(park.Id!, cancellationToken);
        return items
            .OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => new PublicHtmlSitemapNode
            {
                Id = $"park-item:{item.Id}",
                Label = item.Name,
                RelativeUrl = $"{parkPath}/item/{item.Id}/{SeoSlugService.ToSlug(item.Name, "item")}",
                HasChildren = true,
            })
            .ToList();
    }

    private async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildParkZoneNodesAsync(
        string language,
        string parkId,
        CancellationToken cancellationToken)
    {
        Park? park = await this.GetPublicParkAsync(parkId, cancellationToken);
        if (park is null)
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        IReadOnlyCollection<ParkItem> items = await this.GetPublicParkItemsAsync(park.Id!, cancellationToken);
        HashSet<string> visibleZoneIds = items
            .Select(static item => item.ZoneId)
            .Where(static zoneId => !string.IsNullOrWhiteSpace(zoneId))
            .Select(static zoneId => zoneId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        IReadOnlyCollection<ParkZone> zones = await this.parkZoneRepository.GetByParkIdAsync(park.Id!, cancellationToken);
        string parkPath = BuildParkPath(language, park);

        return zones
            .Where(zone => ParkZonesSitemapSectionProvider.IsPublicZone(zone) && visibleZoneIds.Contains(zone.Id!))
            .OrderBy(static zone => zone.SortOrder)
            .ThenBy(static zone => zone.Name, StringComparer.OrdinalIgnoreCase)
            .Select(zone => CreateLeaf(
                $"park-zone:{zone.Id}",
                ResolveLocalizedText(zone.Names, language, zone.Name),
                $"{parkPath}/zone/{zone.Id}/{SeoSlugService.ToSlug(zone.Name, "zone")}"))
            .ToList();
    }

    private async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildParkItemChildNodesAsync(
        string language,
        string itemId,
        CancellationToken cancellationToken)
    {
        ParkItem? item = await this.parkItemRepository.GetByIdAsync(itemId, includeHidden: false, cancellationToken);
        if (item is null || !ParkItemsSitemapSectionProvider.IsPublicItem(item))
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        Park? park = await this.GetPublicParkAsync(item.ParkId, cancellationToken);
        if (park is null)
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        string itemPath = $"{BuildParkPath(language, park)}/item/{item.Id}/{SeoSlugService.ToSlug(item.Name, "item")}";
        List<PublicHtmlSitemapNode> nodes = new List<PublicHtmlSitemapNode>();

        if (await this.HasPublishedImagesAsync(ImageOwnerType.ParkItem, ImageCategory.ParkItem, item.Id!, cancellationToken))
        {
            nodes.Add(CreateLeaf($"park-item-images:{item.Id}", Label(language, "images"), $"{itemPath}/images"));
        }

        if (await this.HasVideosAsync(VideoOwnerType.ParkItem, item.Id!, language, cancellationToken))
        {
            nodes.Add(new PublicHtmlSitemapNode
            {
                Id = $"park-item-videos:{item.Id}",
                Label = Label(language, "videos"),
                RelativeUrl = $"{itemPath}/videos",
                HasChildren = true,
            });
        }

        if (await this.HasParkItemHistoryAsync(item, cancellationToken))
        {
            nodes.Add(new PublicHtmlSitemapNode
            {
                Id = $"park-item-history:{item.Id}",
                Label = Label(language, "history"),
                RelativeUrl = $"{itemPath}/history",
                HasChildren = true,
            });
        }

        return nodes;
    }

    private async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildParkVideoNodesAsync(
        string language,
        string parkId,
        CancellationToken cancellationToken)
    {
        Park? park = await this.GetPublicParkAsync(parkId, cancellationToken);
        if (park is null)
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        string parkPath = BuildParkPath(language, park);
        IReadOnlyCollection<Video> videos = await this.GetVideosAsync(VideoOwnerType.Park, park.Id!, language, cancellationToken);
        return videos
            .OrderBy(static video => video.Title, StringComparer.OrdinalIgnoreCase)
            .Select(video => CreateLeaf(
                $"park-video:{video.Id}",
                ResolveLocalizedText(video.Titles, language, video.Title),
                $"{parkPath}/videos/{video.Id}/{SeoSlugService.ToSlug(video.Title, "video")}"))
            .ToList();
    }

    private async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildParkItemVideoNodesAsync(
        string language,
        string itemId,
        CancellationToken cancellationToken)
    {
        ParkItem? item = await this.parkItemRepository.GetByIdAsync(itemId, includeHidden: false, cancellationToken);
        if (item is null || !ParkItemsSitemapSectionProvider.IsPublicItem(item))
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        Park? park = await this.GetPublicParkAsync(item.ParkId, cancellationToken);
        if (park is null)
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        string itemPath = $"{BuildParkPath(language, park)}/item/{item.Id}/{SeoSlugService.ToSlug(item.Name, "item")}";
        IReadOnlyCollection<Video> videos = await this.GetVideosAsync(VideoOwnerType.ParkItem, item.Id!, language, cancellationToken);
        return videos
            .OrderBy(static video => video.Title, StringComparer.OrdinalIgnoreCase)
            .Select(video => CreateLeaf(
                $"park-item-video:{video.Id}",
                ResolveLocalizedText(video.Titles, language, video.Title),
                $"{itemPath}/videos/{video.Id}/{SeoSlugService.ToSlug(video.Title, "video")}"))
            .ToList();
    }

    private async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildParkHistoryArticleNodesAsync(
        string language,
        string parkId,
        CancellationToken cancellationToken)
    {
        Park? park = await this.GetPublicParkAsync(parkId, cancellationToken);
        if (park is null)
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        IReadOnlyCollection<HistoryEvent> events = await this.historyEventRepository.GetOwnerTimelineAsync(
            HistoryEntityType.Park,
            park.Id!,
            includeHidden: false,
            cancellationToken);
        string parkPath = BuildParkPath(language, park);

        return events
            .Where(HistorySitemapCandidateResolver.IsPublicArticleEvent)
            .OrderByDescending(static historyEvent => historyEvent.Year)
            .ThenBy(static historyEvent => historyEvent.Key, StringComparer.OrdinalIgnoreCase)
            .Select(historyEvent => CreateLeaf(
                $"park-history-article:{historyEvent.Id}",
                ResolveHistoryLabel(historyEvent, language),
                $"{parkPath}/history/{historyEvent.Id}/{HistorySitemapCandidateResolver.ResolveHistoryEventSlug(historyEvent)}"))
            .ToList();
    }

    private async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildParkItemHistoryArticleNodesAsync(
        string language,
        string itemId,
        CancellationToken cancellationToken)
    {
        ParkItem? item = await this.parkItemRepository.GetByIdAsync(itemId, includeHidden: false, cancellationToken);
        if (item is null || !ParkItemsSitemapSectionProvider.IsPublicItem(item))
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        Park? park = await this.GetPublicParkAsync(item.ParkId, cancellationToken);
        if (park is null)
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        IReadOnlyCollection<HistoryEvent> events = await this.historyEventRepository.GetOwnerTimelineAsync(
            HistoryEntityType.ParkItem,
            item.Id!,
            includeHidden: false,
            cancellationToken);
        string itemPath = $"{BuildParkPath(language, park)}/item/{item.Id}/{SeoSlugService.ToSlug(item.Name, "item")}";

        return events
            .Where(HistorySitemapCandidateResolver.IsPublicArticleEvent)
            .OrderByDescending(static historyEvent => historyEvent.Year)
            .ThenBy(static historyEvent => historyEvent.Key, StringComparer.OrdinalIgnoreCase)
            .Select(historyEvent => CreateLeaf(
                $"park-item-history-article:{historyEvent.Id}",
                ResolveHistoryLabel(historyEvent, language),
                $"{itemPath}/history/{historyEvent.Id}/{HistorySitemapCandidateResolver.ResolveHistoryEventSlug(historyEvent)}"))
            .ToList();
    }

    private async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildTechnicalPageNodesAsync(
        string language,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TechnicalPage> pages = await this.technicalPageRepository.GetPublicLinkIndexAsync(cancellationToken);
        return pages
            .Where(static page => !string.IsNullOrWhiteSpace(page.Slug))
            .OrderBy(static page => page.SortOrder)
            .ThenBy(page => ResolveLocalizedText(page.Titles, language, page.Slug), StringComparer.OrdinalIgnoreCase)
            .Select(page => CreateLeaf(
                $"technical-page:{page.Slug}",
                ResolveLocalizedText(page.Titles, language, page.Slug),
                $"/{language}/technical/{page.Slug}"))
            .ToList();
    }
}
