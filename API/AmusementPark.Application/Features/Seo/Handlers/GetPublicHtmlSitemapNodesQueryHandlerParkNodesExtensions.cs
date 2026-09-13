using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;
using AmusementPark.Core.Localization;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using AmusementPark.Core.Domain.TechnicalPages;
using System.Xml;
using System.Xml.Linq;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.TechnicalPages.Ports;
using AmusementPark.Application.Features.Videos.Ports;

namespace AmusementPark.Application.Features.Seo.Handlers;
internal static class GetPublicHtmlSitemapNodesQueryHandlerParkNodesExtensions
{
    internal static async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildParkNodesAsync(this GetPublicHtmlSitemapNodesQueryHandler processorContext, string language, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Park> parks = await SitemapPublicCandidateLoader.LoadPublicParksAsync(processorContext.parkRepository, cancellationToken);
        return parks.OrderBy(static park => park.Name, StringComparer.OrdinalIgnoreCase).Select(park => new PublicHtmlSitemapNode { Id = $"park:{park.Id}", Label = park.Name ?? park.Id ?? GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "park"), RelativeUrl = GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.BuildParkPath(language, park), HasChildren = true, }).ToList();
    }

    internal static async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildParkChildNodesAsync(this GetPublicHtmlSitemapNodesQueryHandler processorContext, string language, string parkId, CancellationToken cancellationToken)
    {
        Park? park = await processorContext.GetPublicParkAsync(parkId, cancellationToken);
        if (park is null)
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        string parkPath = GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.BuildParkPath(language, park);
        IReadOnlyCollection<ParkItem> publicItems = await processorContext.GetPublicParkItemsAsync(park.Id!, cancellationToken);
        IReadOnlyCollection<ParkItem> publicHistoryItems = await processorContext.GetPublicHistoryParkItemsAsync(park.Id!, cancellationToken);
        IReadOnlyCollection<string>? itemIdsWithExplicitHistory = null;
        List<PublicHtmlSitemapNode> nodes = new List<PublicHtmlSitemapNode>();
        if (publicItems.Any(ParksSitemapSectionProvider.HasPublicMapMarker) || park.HasPublicOfficialMaps())
        {
            nodes.Add(GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf($"park-map:{park.Id}", GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "interactiveMap"), $"{parkPath}/map"));
        }

        if (park.Status.IsOpenToVisitors() && GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.HasPosition(park))
        {
            nodes.Add(GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf($"park-weather:{park.Id}", GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "weather"), $"{parkPath}/weather"));
        }

        if (park.Status.CanHaveCurrentOpeningHours() && await processorContext.HasOpeningHoursAsync(park.Id!, cancellationToken))
        {
            nodes.Add(GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf($"park-opening-hours:{park.Id}", GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "openingHours"), $"{parkPath}/opening-hours"));
        }

        if (park.Status.IsOpenToVisitors() && await processorContext.HasCurrentPricingAsync(park.Id!, cancellationToken))
        {
            nodes.Add(GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf($"park-pricing:{park.Id}", GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "pricing"), $"{parkPath}/pricing"));
        }

        if (await processorContext.HasParkImagesAsync(park.Id!, publicItems, cancellationToken))
        {
            nodes.Add(GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf($"park-images:{park.Id}", GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "images"), $"{parkPath}/images"));
        }

        if (await processorContext.HasVideosAsync(VideoOwnerType.Park, park.Id!, language, cancellationToken))
        {
            nodes.Add(new PublicHtmlSitemapNode { Id = $"park-videos:{park.Id}", Label = GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "videos"), RelativeUrl = $"{parkPath}/videos", HasChildren = true, });
        }

        if (await processorContext.HasVisibleZonesAsync(park.Id!, publicItems, cancellationToken))
        {
            nodes.Add(new PublicHtmlSitemapNode { Id = $"park-zones:{park.Id}", Label = GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "zones"), RelativeUrl = $"{parkPath}/zones", HasChildren = true, });
        }

        bool hasItemBranch = publicItems.Count > 0 || publicHistoryItems.Any(AutomaticHistoryEventFactory.HasLifecycleDate);
        if (!hasItemBranch)
        {
            itemIdsWithExplicitHistory = await processorContext.ResolveItemIdsWithExplicitHistoryAsync(park.Id!, publicHistoryItems, cancellationToken);
            hasItemBranch = itemIdsWithExplicitHistory.Count > 0;
        }

        if (hasItemBranch)
        {
            nodes.Add(new PublicHtmlSitemapNode { Id = $"park-items:{park.Id}", Label = GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "items"), RelativeUrl = $"{parkPath}/items", HasChildren = true, });
        }

        if (await processorContext.HasParkHistoryAsync(park, publicHistoryItems, itemIdsWithExplicitHistory, cancellationToken))
        {
            nodes.Add(new PublicHtmlSitemapNode { Id = $"park-history:{park.Id}", Label = GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "history"), RelativeUrl = $"{parkPath}/history", HasChildren = true, });
        }

        return nodes;
    }

    internal static async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildParkItemNodesAsync(this GetPublicHtmlSitemapNodesQueryHandler processorContext, string language, string parkId, CancellationToken cancellationToken)
    {
        Park? park = await processorContext.GetPublicParkAsync(parkId, cancellationToken);
        if (park is null)
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        string parkPath = GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.BuildParkPath(language, park);
        IReadOnlyCollection<ParkItem> items = await processorContext.GetPublicHistoryParkItemsAsync(park.Id!, cancellationToken);
        HashSet<string> itemIdsWithExplicitHistory = await processorContext.ResolveItemIdsWithExplicitHistoryAsync(park.Id!, items, cancellationToken);
        List<PublicHtmlSitemapNode> nodes = new List<PublicHtmlSitemapNode>();
        foreach (ParkItem item in items.OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            bool isPublicItem = ParkItemsSitemapSectionProvider.IsPublicItem(item);
            bool hasHistory = isPublicItem || AutomaticHistoryEventFactory.HasLifecycleDate(item) || (!string.IsNullOrWhiteSpace(item.Id) && itemIdsWithExplicitHistory.Contains(item.Id));
            if (!isPublicItem && !hasHistory)
            {
                continue;
            }

            string itemPath = $"{parkPath}/item/{item.Id}/{SeoSlugService.ToSlug(item.Name, "item")}";
            nodes.Add(new PublicHtmlSitemapNode { Id = $"park-item:{item.Id}", Label = item.Name, RelativeUrl = isPublicItem ? itemPath : $"{itemPath}/history", HasChildren = true, });
        }

        return nodes;
    }

    internal static async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildParkZoneNodesAsync(this GetPublicHtmlSitemapNodesQueryHandler processorContext, string language, string parkId, CancellationToken cancellationToken)
    {
        Park? park = await processorContext.GetPublicParkAsync(parkId, cancellationToken);
        if (park is null)
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        IReadOnlyCollection<ParkItem> items = await processorContext.GetPublicParkItemsAsync(park.Id!, cancellationToken);
        HashSet<string> visibleZoneIds = items.Select(static item => item.ZoneId).Where(static zoneId => !string.IsNullOrWhiteSpace(zoneId)).Select(static zoneId => zoneId!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        IReadOnlyCollection<ParkZone> zones = await processorContext.parkZoneRepository.GetByParkIdAsync(park.Id!, cancellationToken);
        string parkPath = GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.BuildParkPath(language, park);
        return zones.Where(zone => ParkZonesSitemapSectionProvider.IsPublicZone(zone) && visibleZoneIds.Contains(zone.Id!)).OrderBy(static zone => zone.SortOrder).ThenBy(static zone => zone.Name, StringComparer.OrdinalIgnoreCase).Select(zone => GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf($"park-zone:{zone.Id}", GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.ResolveLocalizedText(zone.Names, language, zone.Name), $"{parkPath}/zone/{zone.Id}/{SeoSlugService.ToSlug(zone.Name, "zone")}")).ToList();
    }

    internal static async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildParkItemChildNodesAsync(this GetPublicHtmlSitemapNodesQueryHandler processorContext, string language, string itemId, CancellationToken cancellationToken)
    {
        ParkItem? item = await processorContext.parkItemRepository.GetByIdAsync(itemId, includeHidden: false, cancellationToken);
        if (item is null || !HistorySitemapCandidateResolver.IsPublicHistoryItem(item))
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        Park? park = await processorContext.GetPublicParkAsync(item.ParkId, cancellationToken);
        if (park is null)
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        string itemPath = $"{GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.BuildParkPath(language, park)}/item/{item.Id}/{SeoSlugService.ToSlug(item.Name, "item")}";
        List<PublicHtmlSitemapNode> nodes = new List<PublicHtmlSitemapNode>();
        bool isPublicItem = ParkItemsSitemapSectionProvider.IsPublicItem(item);
        if (isPublicItem && await processorContext.HasPublishedImagesAsync(ImageOwnerType.ParkItem, ImageCategory.ParkItem, item.Id!, cancellationToken))
        {
            nodes.Add(GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf($"park-item-images:{item.Id}", GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "images"), $"{itemPath}/images"));
        }

        if (isPublicItem && await processorContext.HasVideosAsync(VideoOwnerType.ParkItem, item.Id!, language, cancellationToken))
        {
            nodes.Add(new PublicHtmlSitemapNode { Id = $"park-item-videos:{item.Id}", Label = GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "videos"), RelativeUrl = $"{itemPath}/videos", HasChildren = true, });
        }

        if (await processorContext.HasParkItemHistoryAsync(item, cancellationToken))
        {
            nodes.Add(new PublicHtmlSitemapNode { Id = $"park-item-history:{item.Id}", Label = GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "history"), RelativeUrl = $"{itemPath}/history", HasChildren = true, });
        }

        return nodes;
    }

    internal static async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildParkVideoNodesAsync(this GetPublicHtmlSitemapNodesQueryHandler processorContext, string language, string parkId, CancellationToken cancellationToken)
    {
        Park? park = await processorContext.GetPublicParkAsync(parkId, cancellationToken);
        if (park is null)
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        string parkPath = GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.BuildParkPath(language, park);
        IReadOnlyCollection<Video> videos = await processorContext.GetVideosAsync(VideoOwnerType.Park, park.Id!, language, cancellationToken);
        return videos.OrderBy(static video => video.Title, StringComparer.OrdinalIgnoreCase).Select(video => GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf($"park-video:{video.Id}", GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.ResolveLocalizedText(video.Titles, language, video.Title), $"{parkPath}/videos/{video.Id}/{SeoSlugService.ToSlug(video.Title, "video")}")).ToList();
    }

    internal static async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildParkItemVideoNodesAsync(this GetPublicHtmlSitemapNodesQueryHandler processorContext, string language, string itemId, CancellationToken cancellationToken)
    {
        ParkItem? item = await processorContext.parkItemRepository.GetByIdAsync(itemId, includeHidden: false, cancellationToken);
        if (item is null || !HistorySitemapCandidateResolver.IsPublicHistoryItem(item))
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        Park? park = await processorContext.GetPublicParkAsync(item.ParkId, cancellationToken);
        if (park is null)
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        string itemPath = $"{GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.BuildParkPath(language, park)}/item/{item.Id}/{SeoSlugService.ToSlug(item.Name, "item")}";
        IReadOnlyCollection<Video> videos = await processorContext.GetVideosAsync(VideoOwnerType.ParkItem, item.Id!, language, cancellationToken);
        return videos.OrderBy(static video => video.Title, StringComparer.OrdinalIgnoreCase).Select(video => GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf($"park-item-video:{video.Id}", GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.ResolveLocalizedText(video.Titles, language, video.Title), $"{itemPath}/videos/{video.Id}/{SeoSlugService.ToSlug(video.Title, "video")}")).ToList();
    }

    internal static async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildParkHistoryArticleNodesAsync(this GetPublicHtmlSitemapNodesQueryHandler processorContext, string language, string parkId, CancellationToken cancellationToken)
    {
        Park? park = await processorContext.GetPublicParkAsync(parkId, cancellationToken);
        if (park is null)
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        IReadOnlyCollection<HistoryEvent> events = await processorContext.historyEventRepository.GetOwnerTimelineSummaryAsync(HistoryEntityType.Park, park.Id!, includeHidden: false, cancellationToken);
        string parkPath = GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.BuildParkPath(language, park);
        return events.Where(HistorySitemapCandidateResolver.IsPublicArticleEvent).OrderByDescending(static historyEvent => historyEvent.Year).ThenBy(static historyEvent => historyEvent.Key, StringComparer.OrdinalIgnoreCase).Select(historyEvent => GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf($"park-history-article:{historyEvent.Id}", GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.ResolveHistoryLabel(historyEvent, language), $"{parkPath}/history/{historyEvent.Id}/{HistorySitemapCandidateResolver.ResolveHistoryEventSlug(historyEvent)}")).ToList();
    }

    internal static async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildParkItemHistoryArticleNodesAsync(this GetPublicHtmlSitemapNodesQueryHandler processorContext, string language, string itemId, CancellationToken cancellationToken)
    {
        ParkItem? item = await processorContext.parkItemRepository.GetByIdAsync(itemId, includeHidden: false, cancellationToken);
        if (item is null || !ParkItemsSitemapSectionProvider.IsPublicItem(item))
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        Park? park = await processorContext.GetPublicParkAsync(item.ParkId, cancellationToken);
        if (park is null)
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }

        IReadOnlyCollection<HistoryEvent> events = await processorContext.historyEventRepository.GetOwnerTimelineSummaryAsync(HistoryEntityType.ParkItem, item.Id!, includeHidden: false, cancellationToken);
        string itemPath = $"{GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.BuildParkPath(language, park)}/item/{item.Id}/{SeoSlugService.ToSlug(item.Name, "item")}";
        return events.Where(HistorySitemapCandidateResolver.IsPublicArticleEvent).OrderByDescending(static historyEvent => historyEvent.Year).ThenBy(static historyEvent => historyEvent.Key, StringComparer.OrdinalIgnoreCase).Select(historyEvent => GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf($"park-item-history-article:{historyEvent.Id}", GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.ResolveHistoryLabel(historyEvent, language), $"{itemPath}/history/{historyEvent.Id}/{HistorySitemapCandidateResolver.ResolveHistoryEventSlug(historyEvent)}")).ToList();
    }

    internal static async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildTechnicalPageNodesAsync(this GetPublicHtmlSitemapNodesQueryHandler processorContext, string language, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TechnicalPage> pages = await processorContext.technicalPageRepository.GetPublicLinkIndexAsync(cancellationToken);
        return pages.Where(static page => !string.IsNullOrWhiteSpace(page.Slug)).OrderBy(static page => page.SortOrder).ThenBy(page => GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.ResolveLocalizedText(page.Titles, language, page.Slug), StringComparer.OrdinalIgnoreCase).Select(page => GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf($"technical-page:{page.Slug}", GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.ResolveLocalizedText(page.Titles, language, page.Slug), $"/{language}/technical/{page.Slug}")).ToList();
    }
}
