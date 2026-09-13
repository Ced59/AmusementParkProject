using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Seo.Services;
public sealed class PublicSeoUrlResolver
{
    internal readonly IParkItemRepository parkItemRepository;
    internal readonly IParkRepository parkRepository;
    internal readonly IParkZoneRepository parkZoneRepository;
    internal readonly IImageRepository imageRepository;
    public PublicSeoUrlResolver(IParkItemRepository parkItemRepository, IParkRepository parkRepository, IParkZoneRepository parkZoneRepository, IImageRepository imageRepository)
    {
        this.parkItemRepository = parkItemRepository;
        this.parkRepository = parkRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.imageRepository = imageRepository;
    }

    public async Task<IReadOnlyCollection<string>> ResolveAsync(PublicSeoUpdate update, IReadOnlyCollection<string> supportedLanguages, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);
        IReadOnlyCollection<string> languages = ParksSitemapSectionProvider.NormalizeLanguages(supportedLanguages);
        HashSet<string> relativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> imageCountByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        IReadOnlyCollection<PublicSeoParkSnapshot> parkSnapshots = PublicSeoUrlResolver.MergeParkSnapshots(update.PreviousParks, update.CurrentParks);
        IReadOnlyCollection<string> changedParkIds = parkSnapshots.Select(static park => park.Id).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        PublicSeoParkItemsByParkId currentItemsByParkId = await this.LoadCurrentItemsByParkIdAsync(changedParkIds, cancellationToken);
        IReadOnlyDictionary<string, IReadOnlyCollection<PublicSeoParkZoneSnapshot>> zonesByParkId = await this.LoadZonesByParkIdAsync(changedParkIds, cancellationToken);
        foreach (PublicSeoParkSnapshot park in parkSnapshots)
        {
            if (!PublicSeoUrlResolver.IsPublicPark(park) && !PublicSeoUrlResolver.IsPublicHistoryPark(park))
            {
                continue;
            }

            IReadOnlyCollection<PublicSeoParkItemSnapshot> currentPublicItems = currentItemsByParkId.PublicItemsByParkId.GetValueOrDefault(park.Id) ?? new List<PublicSeoParkItemSnapshot>();
            IReadOnlyCollection<PublicSeoParkItemSnapshot> currentHistoryItems = currentItemsByParkId.HistoryItemsByParkId.GetValueOrDefault(park.Id) ?? new List<PublicSeoParkItemSnapshot>();
            if (!PublicSeoUrlResolver.IsPublicPark(park))
            {
                if (PublicSeoUrlResolver.HasParkLifecycleDate(park) || currentHistoryItems.Any(PublicSeoUrlResolver.HasParkItemLifecycleDate))
                {
                    PublicSeoUrlResolverRoutesExtensions.AddParkHistoryUrls(relativePaths, languages, park);
                }

                foreach (PublicSeoParkItemSnapshot item in currentHistoryItems)
                {
                    if (PublicSeoUrlResolver.HasParkItemLifecycleDate(item))
                    {
                        PublicSeoUrlResolverRoutesExtensions.AddParkItemHistoryUrls(relativePaths, languages, park, item);
                    }
                }

                continue;
            }

            await this.AddParkImpactUrlsAsync(relativePaths, languages, park, currentPublicItems, currentHistoryItems, PublicSeoUrlResolver.MergeZoneSnapshots(zonesByParkId.GetValueOrDefault(park.Id) ?? Array.Empty<PublicSeoParkZoneSnapshot>(), update.PreviousParkZones.Where(zone => string.Equals(zone.ParkId, park.Id, StringComparison.OrdinalIgnoreCase)), update.CurrentParkZones.Where(zone => string.Equals(zone.ParkId, park.Id, StringComparison.OrdinalIgnoreCase))), imageCountByKey, cancellationToken);
        }

        if (update.IncludeDiscoveryPages && parkSnapshots.Any(PublicSeoUrlResolver.IsPublicPark))
        {
            PublicSeoUrlResolverRoutesExtensions.AddDiscoveryUrls(relativePaths, languages);
        }

        IReadOnlyCollection<PublicSeoParkItemSnapshot> itemSnapshots = PublicSeoUrlResolver.MergeItemSnapshots(update.PreviousParkItems, update.CurrentParkItems);
        IReadOnlyCollection<string> parentParkIds = itemSnapshots.Where(static item => PublicSeoUrlResolver.IsPublicItem(item) || PublicSeoUrlResolver.IsPublicHistoryItem(item)).Select(static item => item.ParkId).Where(static parkId => !string.IsNullOrWhiteSpace(parkId)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        IReadOnlyDictionary<string, PublicSeoParkSnapshot> parentParkById = await this.LoadParentParksAsync(parentParkIds, update, cancellationToken);
        IReadOnlyDictionary<string, IReadOnlyCollection<PublicSeoParkZoneSnapshot>> parentZonesByParkId = await this.LoadZonesByParkIdAsync(parentParkIds, cancellationToken);
        foreach (PublicSeoParkItemSnapshot item in itemSnapshots)
        {
            if (!parentParkById.TryGetValue(item.ParkId, out PublicSeoParkSnapshot? parentPark) || !PublicSeoUrlResolver.IsPublicHistoryPark(parentPark))
            {
                continue;
            }

            bool isPublicItem = PublicSeoUrlResolver.IsPublicItem(item);
            bool isPublicHistoryItem = PublicSeoUrlResolver.IsPublicHistoryItem(item);
            if (!isPublicItem && !isPublicHistoryItem)
            {
                continue;
            }

            if (isPublicHistoryItem && PublicSeoUrlResolver.HasParkItemLifecycleDate(item))
            {
                PublicSeoUrlResolverRoutesExtensions.AddParkHistoryUrls(relativePaths, languages, parentPark);
                PublicSeoUrlResolverRoutesExtensions.AddParkItemHistoryUrls(relativePaths, languages, parentPark, item);
            }

            if (!PublicSeoUrlResolver.IsPublicPark(parentPark) || !isPublicItem)
            {
                continue;
            }

            PublicSeoUrlResolverRoutesExtensions.AddParkDetailUrls(relativePaths, languages, parentPark);
            PublicSeoUrlResolverRoutesExtensions.AddParkItemListUrls(relativePaths, languages, parentPark);
            if (PublicSeoUrlResolver.HasPublicMapMarker(item))
            {
                PublicSeoUrlResolverRoutesExtensions.AddParkMapUrls(relativePaths, languages, parentPark);
            }

            IReadOnlyCollection<PublicSeoParkZoneSnapshot> parentZones = PublicSeoUrlResolver.MergeZoneSnapshots(parentZonesByParkId.GetValueOrDefault(item.ParkId) ?? Array.Empty<PublicSeoParkZoneSnapshot>(), update.PreviousParkZones.Where(zone => string.Equals(zone.ParkId, item.ParkId, StringComparison.OrdinalIgnoreCase)), update.CurrentParkZones.Where(zone => string.Equals(zone.ParkId, item.ParkId, StringComparison.OrdinalIgnoreCase)));
            PublicSeoUrlResolverRoutesExtensions.AddZoneImpactUrls(relativePaths, languages, parentPark, new[] { item }, parentZones);
            PublicSeoUrlResolverRoutesExtensions.AddParkItemDetailUrls(relativePaths, languages, parentPark, item);
            bool itemHasImages = SeoPageValuePolicy.IsImageGalleryIndexable(await this.GetPublishedImageCountAsync(ImageOwnerType.ParkItem, ImageCategory.ParkItem, item.Id, imageCountByKey, cancellationToken));
            if (itemHasImages)
            {
                PublicSeoUrlResolverRoutesExtensions.AddParkImageUrls(relativePaths, languages, parentPark);
                PublicSeoUrlResolverRoutesExtensions.AddParkItemImageUrls(relativePaths, languages, parentPark, item);
            }
        }

        IReadOnlyCollection<PublicSeoVideoSnapshot> videoSnapshots = PublicSeoUrlResolver.MergeVideoSnapshots(update.PreviousVideos, update.CurrentVideos);
        await this.AddVideoImpactUrlsAsync(relativePaths, languages, videoSnapshots, update, cancellationToken);
        IReadOnlyCollection<PublicSeoImageSnapshot> imageSnapshots = PublicSeoUrlResolverImagesExtensions.MergeImageSnapshots(update.PreviousImages, update.CurrentImages);
        await this.AddImageImpactUrlsAsync(relativePaths, languages, imageSnapshots, update, cancellationToken);
        return relativePaths.OrderBy(static path => path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    internal async Task AddParkImpactUrlsAsync(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park, IReadOnlyCollection<PublicSeoParkItemSnapshot> currentPublicItems, IReadOnlyCollection<PublicSeoParkItemSnapshot> currentHistoryItems, IReadOnlyCollection<PublicSeoParkZoneSnapshot> zones, Dictionary<string, int> imageCountByKey, CancellationToken cancellationToken)
    {
        PublicSeoUrlResolverRoutesExtensions.AddParkDetailUrls(relativePaths, languages, park);
        if (park.HasPublicOfficialMaps || SeoPageValuePolicy.IsCollectionIndexable(currentPublicItems.Count(PublicSeoUrlResolver.HasPublicMapMarker)))
        {
            PublicSeoUrlResolverRoutesExtensions.AddParkMapUrls(relativePaths, languages, park);
        }

        if (PublicSeoUrlResolver.HasParkLifecycleDate(park) || currentHistoryItems.Any(PublicSeoUrlResolver.HasParkItemLifecycleDate))
        {
            PublicSeoUrlResolverRoutesExtensions.AddParkHistoryUrls(relativePaths, languages, park);
        }

        if (await this.HasMinimumPublishedParkOrItemImagesAsync(park.Id, currentPublicItems, imageCountByKey, cancellationToken))
        {
            PublicSeoUrlResolverRoutesExtensions.AddParkImageUrls(relativePaths, languages, park);
        }

        if (SeoPageValuePolicy.IsCollectionIndexable(currentPublicItems.Count))
        {
            PublicSeoUrlResolverRoutesExtensions.AddParkItemListUrls(relativePaths, languages, park);
        }

        PublicSeoUrlResolverRoutesExtensions.AddZoneImpactUrls(relativePaths, languages, park, currentPublicItems, zones);
        foreach (PublicSeoParkItemSnapshot item in currentPublicItems)
        {
            PublicSeoUrlResolverRoutesExtensions.AddParkItemDetailUrls(relativePaths, languages, park, item);
            int itemImageCount = await this.GetPublishedImageCountAsync(ImageOwnerType.ParkItem, ImageCategory.ParkItem, item.Id, imageCountByKey, cancellationToken);
            if (SeoPageValuePolicy.IsImageGalleryIndexable(itemImageCount))
            {
                PublicSeoUrlResolverRoutesExtensions.AddParkItemImageUrls(relativePaths, languages, park, item);
            }
        }

        foreach (PublicSeoParkItemSnapshot item in currentHistoryItems)
        {
            if (PublicSeoUrlResolver.HasParkItemLifecycleDate(item))
            {
                PublicSeoUrlResolverRoutesExtensions.AddParkItemHistoryUrls(relativePaths, languages, park, item);
            }
        }
    }

    internal async Task<PublicSeoParkItemsByParkId> LoadCurrentItemsByParkIdAsync(IReadOnlyCollection<string> parkIds, CancellationToken cancellationToken)
    {
        Dictionary<string, List<PublicSeoParkItemSnapshot>> publicItemsByParkId = new Dictionary<string, List<PublicSeoParkItemSnapshot>>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, List<PublicSeoParkItemSnapshot>> historyItemsByParkId = new Dictionary<string, List<PublicSeoParkItemSnapshot>>(StringComparer.OrdinalIgnoreCase);
        foreach (string parkId in parkIds)
        {
            IReadOnlyCollection<ParkItem> items = await this.parkItemRepository.GetByParkIdAsync(parkId, true, cancellationToken);
            foreach (ParkItem item in items)
            {
                PublicSeoParkItemSnapshot? snapshot = PublicSeoParkItemSnapshot.FromParkItem(item);
                if (snapshot is null)
                {
                    continue;
                }

                if (PublicSeoUrlResolver.IsPublicItem(snapshot))
                {
                    PublicSeoUrlResolver.AddItemSnapshot(publicItemsByParkId, snapshot);
                }

                if (PublicSeoUrlResolver.IsPublicHistoryItem(snapshot))
                {
                    PublicSeoUrlResolver.AddItemSnapshot(historyItemsByParkId, snapshot);
                }
            }
        }

        return new PublicSeoParkItemsByParkId(publicItemsByParkId, historyItemsByParkId);
    }

    internal static void AddItemSnapshot(IDictionary<string, List<PublicSeoParkItemSnapshot>> itemsByParkId, PublicSeoParkItemSnapshot snapshot)
    {
        if (!itemsByParkId.TryGetValue(snapshot.ParkId, out List<PublicSeoParkItemSnapshot>? parkItems))
        {
            parkItems = new List<PublicSeoParkItemSnapshot>();
            itemsByParkId[snapshot.ParkId] = parkItems;
        }

        parkItems.Add(snapshot);
    }

    internal async Task AddVideoImpactUrlsAsync(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, IReadOnlyCollection<PublicSeoVideoSnapshot> videoSnapshots, PublicSeoUpdate update, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<PublicSeoVideoSnapshot> publicVideos = videoSnapshots.Where(PublicSeoUrlResolver.IsPublicVideo).ToList();
        if (publicVideos.Count == 0)
        {
            return;
        }

        IReadOnlyCollection<PublicSeoVideoSnapshot> parkVideos = publicVideos.Where(static video => video.OwnerType == VideoOwnerType.Park).ToList();
        IReadOnlyCollection<string> parkOwnerIds = parkVideos.Select(static video => video.OwnerId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        IReadOnlyDictionary<string, PublicSeoParkSnapshot> parksById = await this.LoadParentParksAsync(parkOwnerIds, update, cancellationToken);
        foreach (PublicSeoVideoSnapshot video in parkVideos)
        {
            if (!parksById.TryGetValue(video.OwnerId, out PublicSeoParkSnapshot? park) || !PublicSeoUrlResolver.IsPublicPark(park))
            {
                continue;
            }

            PublicSeoUrlResolverRoutesExtensions.AddParkDetailUrls(relativePaths, languages, park);
            IReadOnlyCollection<string> videoLanguages = PublicSeoUrlResolver.ResolveVisibleLanguages(video, languages);
            PublicSeoUrlResolverRoutesExtensions.AddParkVideoUrls(relativePaths, videoLanguages, park);
            PublicSeoUrlResolverRoutesExtensions.AddParkVideoDetailUrls(relativePaths, videoLanguages, park, video);
        }

        IReadOnlyCollection<PublicSeoVideoSnapshot> itemVideos = publicVideos.Where(static video => video.OwnerType == VideoOwnerType.ParkItem).ToList();
        IReadOnlyCollection<string> itemOwnerIds = itemVideos.Select(static video => video.OwnerId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        IReadOnlyDictionary<string, PublicSeoParkItemSnapshot> itemsById = await this.LoadVideoOwnerItemsAsync(itemOwnerIds, cancellationToken);
        IReadOnlyCollection<string> itemParkIds = itemsById.Values.Where(PublicSeoUrlResolver.IsPublicItem).Select(static item => item.ParkId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        IReadOnlyDictionary<string, PublicSeoParkSnapshot> itemParksById = await this.LoadParentParksAsync(itemParkIds, update, cancellationToken);
        foreach (PublicSeoVideoSnapshot video in itemVideos)
        {
            if (!itemsById.TryGetValue(video.OwnerId, out PublicSeoParkItemSnapshot? item) || !PublicSeoUrlResolver.IsPublicItem(item))
            {
                continue;
            }

            if (!itemParksById.TryGetValue(item.ParkId, out PublicSeoParkSnapshot? park) || !PublicSeoUrlResolver.IsPublicPark(park))
            {
                continue;
            }

            PublicSeoUrlResolverRoutesExtensions.AddParkDetailUrls(relativePaths, languages, park);
            PublicSeoUrlResolverRoutesExtensions.AddParkItemDetailUrls(relativePaths, languages, park, item);
            IReadOnlyCollection<string> videoLanguages = PublicSeoUrlResolver.ResolveVisibleLanguages(video, languages);
            PublicSeoUrlResolverRoutesExtensions.AddParkItemVideoUrls(relativePaths, videoLanguages, park, item);
            PublicSeoUrlResolverRoutesExtensions.AddParkItemVideoDetailUrls(relativePaths, videoLanguages, park, item, video);
        }
    }

    internal async Task<IReadOnlyDictionary<string, PublicSeoParkItemSnapshot>> LoadVideoOwnerItemsAsync(IReadOnlyCollection<string> itemIds, CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0)
        {
            return new Dictionary<string, PublicSeoParkItemSnapshot>(StringComparer.OrdinalIgnoreCase);
        }

        IReadOnlyCollection<ParkItem> items = await this.parkItemRepository.GetByIdsAsync(itemIds.ToList(), cancellationToken);
        Dictionary<string, PublicSeoParkItemSnapshot> itemsById = new Dictionary<string, PublicSeoParkItemSnapshot>(StringComparer.OrdinalIgnoreCase);
        foreach (ParkItem item in items)
        {
            PublicSeoParkItemSnapshot? snapshot = PublicSeoParkItemSnapshot.FromParkItem(item);
            if (snapshot is not null && !itemsById.ContainsKey(snapshot.Id))
            {
                itemsById[snapshot.Id] = snapshot;
            }
        }

        return itemsById;
    }

    internal async Task<IReadOnlyDictionary<string, PublicSeoParkSnapshot>> LoadParentParksAsync(IReadOnlyCollection<string> parentParkIds, PublicSeoUpdate update, CancellationToken cancellationToken)
    {
        Dictionary<string, PublicSeoParkSnapshot> parentParkById = new Dictionary<string, PublicSeoParkSnapshot>(StringComparer.OrdinalIgnoreCase);
        foreach (PublicSeoParkSnapshot park in update.PreviousParks.Concat(update.CurrentParks))
        {
            if (!parentParkById.ContainsKey(park.Id))
            {
                parentParkById[park.Id] = park;
            }
        }

        IReadOnlyCollection<string> missingParkIds = parentParkIds.Where(parkId => !parentParkById.ContainsKey(parkId)).ToList();
        if (missingParkIds.Count == 0)
        {
            return parentParkById;
        }

        IReadOnlyCollection<Park> parentParks = await this.parkRepository.GetByIdsAsync(missingParkIds, cancellationToken);
        foreach (Park parentPark in parentParks)
        {
            PublicSeoParkSnapshot? snapshot = PublicSeoParkSnapshot.FromPark(parentPark);
            if (snapshot is not null && !parentParkById.ContainsKey(snapshot.Id))
            {
                parentParkById[snapshot.Id] = snapshot;
            }
        }

        return parentParkById;
    }

    internal static IReadOnlyCollection<PublicSeoParkZoneSnapshot> MergeZoneSnapshots(params IEnumerable<PublicSeoParkZoneSnapshot>[] snapshotGroups)
    {
        List<PublicSeoParkZoneSnapshot> snapshots = new List<PublicSeoParkZoneSnapshot>();
        HashSet<string> keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (IEnumerable<PublicSeoParkZoneSnapshot> snapshotGroup in snapshotGroups)
        {
            foreach (PublicSeoParkZoneSnapshot snapshot in snapshotGroup)
            {
                string key = $"{snapshot.Id}|{snapshot.ParkId}|{snapshot.Name}|{snapshot.IsVisible}";
                if (keys.Add(key))
                {
                    snapshots.Add(snapshot);
                }
            }
        }

        return snapshots;
    }

    internal async Task<IReadOnlyDictionary<string, IReadOnlyCollection<PublicSeoParkZoneSnapshot>>> LoadZonesByParkIdAsync(IReadOnlyCollection<string> parkIds, CancellationToken cancellationToken)
    {
        Dictionary<string, IReadOnlyCollection<PublicSeoParkZoneSnapshot>> zonesByParkId = new Dictionary<string, IReadOnlyCollection<PublicSeoParkZoneSnapshot>>(StringComparer.OrdinalIgnoreCase);
        foreach (string parkId in parkIds)
        {
            IReadOnlyCollection<ParkZone> zones = await this.parkZoneRepository.GetByParkIdAsync(parkId, cancellationToken);
            zonesByParkId[parkId] = PublicSeoParkZoneSnapshot.FromParkZones(zones);
        }

        return zonesByParkId;
    }

    internal static IReadOnlyCollection<PublicSeoParkSnapshot> MergeParkSnapshots(IReadOnlyCollection<PublicSeoParkSnapshot> previousParks, IReadOnlyCollection<PublicSeoParkSnapshot> currentParks)
    {
        return previousParks.Concat(currentParks).Where(static park => !string.IsNullOrWhiteSpace(park.Id)).GroupBy(static park => PublicSeoUrlResolver.BuildParkRouteKey(park), StringComparer.OrdinalIgnoreCase).Select(static group => group.First()).ToList();
    }

    internal static IReadOnlyCollection<PublicSeoParkItemSnapshot> MergeItemSnapshots(IReadOnlyCollection<PublicSeoParkItemSnapshot> previousItems, IReadOnlyCollection<PublicSeoParkItemSnapshot> currentItems)
    {
        return previousItems.Concat(currentItems).Where(static item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.ParkId)).GroupBy(static item => PublicSeoUrlResolver.BuildItemRouteKey(item), StringComparer.OrdinalIgnoreCase).Select(static group => group.First()).ToList();
    }

    internal static IReadOnlyCollection<PublicSeoVideoSnapshot> MergeVideoSnapshots(IReadOnlyCollection<PublicSeoVideoSnapshot> previousVideos, IReadOnlyCollection<PublicSeoVideoSnapshot> currentVideos)
    {
        return previousVideos.Concat(currentVideos).Where(static video => !string.IsNullOrWhiteSpace(video.Id) && !string.IsNullOrWhiteSpace(video.OwnerId)).GroupBy(static video => PublicSeoUrlResolver.BuildVideoRouteKey(video), StringComparer.OrdinalIgnoreCase).Select(static group => group.First()).ToList();
    }

    internal static bool IsPublicPark(PublicSeoParkSnapshot park)
    {
        return !string.IsNullOrWhiteSpace(park.Id) && !string.IsNullOrWhiteSpace(park.Name) && park.IsVisible && park.Status.CanAppearInPublicDiscovery() && park.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    internal static bool IsPublicItem(PublicSeoParkItemSnapshot item)
    {
        return !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.ParkId) && !string.IsNullOrWhiteSpace(item.Name) && item.IsVisible && !ParkItemStatusNormalizer.IsClosedDefinitively(item.Status) && item.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    internal static bool IsPublicHistoryPark(PublicSeoParkSnapshot park)
    {
        return !string.IsNullOrWhiteSpace(park.Id) && !string.IsNullOrWhiteSpace(park.Name) && park.IsVisible && park.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    internal static bool IsPublicHistoryItem(PublicSeoParkItemSnapshot item)
    {
        return !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.ParkId) && !string.IsNullOrWhiteSpace(item.Name) && item.IsVisible && item.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    internal static bool IsPublicVideo(PublicSeoVideoSnapshot video)
    {
        return !string.IsNullOrWhiteSpace(video.Id) && !string.IsNullOrWhiteSpace(video.OwnerId) && (video.OwnerType == VideoOwnerType.Park || video.OwnerType == VideoOwnerType.ParkItem) && video.IsPublished;
    }

    internal static bool HasParkLifecycleDate(PublicSeoParkSnapshot park)
    {
        return park.OpeningDate.HasValue || park.ClosingDate.HasValue || AutomaticHistoryEventFactory.HasLifecycleDateText(park.OpeningDateText) || AutomaticHistoryEventFactory.HasLifecycleDateText(park.ClosingDateText);
    }

    internal static bool HasParkItemLifecycleDate(PublicSeoParkItemSnapshot item)
    {
        return item.OpeningDate.HasValue || item.ClosingDate.HasValue || AutomaticHistoryEventFactory.HasLifecycleDateText(item.OpeningDateText) || AutomaticHistoryEventFactory.HasLifecycleDateText(item.ClosingDateText);
    }

    internal static bool HasPublicMapMarker(PublicSeoParkItemSnapshot item)
    {
        return PublicSeoUrlResolver.IsPublicItem(item) && item.HasPosition;
    }

    internal static string BuildParkRouteKey(PublicSeoParkSnapshot park)
    {
        return $"{park.Id}:{SeoSlugService.ToSlug(park.Name, "park")}:{park.IsVisible}:{park.Status}:{park.AdminReviewStatus}:{park.OpeningDate?.Ticks}:{park.ClosingDate?.Ticks}:{park.OpeningDateText}:{park.ClosingDateText}:{park.HasPublicOfficialMaps}";
    }

    internal static string BuildItemRouteKey(PublicSeoParkItemSnapshot item)
    {
        return $"{item.ParkId}:{item.Id}:{item.ZoneId}:{SeoSlugService.ToSlug(item.Name, "item")}:{item.IsVisible}:{item.Status}:{item.AdminReviewStatus}:{item.OpeningDate?.Ticks}:{item.ClosingDate?.Ticks}:{item.OpeningDateText}:{item.ClosingDateText}:{item.HasPosition}";
    }

    internal static string BuildVideoRouteKey(PublicSeoVideoSnapshot video)
    {
        return $"{video.OwnerType}:{video.OwnerId}:{video.Id}:{SeoSlugService.ToSlug(video.Title, "video")}:{video.IsPublished}:{string.Join(",", PublicSeoUrlResolver.NormalizeVideoLanguageCodes(video.LanguageCodes))}";
    }

    internal static IReadOnlyCollection<string> ResolveVisibleLanguages(PublicSeoVideoSnapshot video, IReadOnlyCollection<string> languages)
    {
        IReadOnlyCollection<string> videoLanguageCodes = PublicSeoUrlResolver.NormalizeVideoLanguageCodes(video.LanguageCodes);
        if (videoLanguageCodes.Count == 0)
        {
            return languages;
        }

        return languages.Where(language => videoLanguageCodes.Contains(PublicSeoUrlResolver.NormalizeLanguageCode(language), StringComparer.Ordinal)).ToList();
    }

    internal static IReadOnlyCollection<string> NormalizeVideoLanguageCodes(IReadOnlyCollection<string> languageCodes)
    {
        return languageCodes.Where(static languageCode => !string.IsNullOrWhiteSpace(languageCode)).Select(static languageCode => PublicSeoUrlResolver.NormalizeLanguageCode(languageCode)).Where(static languageCode => languageCode.Length == 2).Distinct(StringComparer.Ordinal).ToList();
    }

    internal static string NormalizeLanguageCode(string languageCode)
    {
        string normalizedLanguageCode = languageCode.Trim().ToLowerInvariant();
        return normalizedLanguageCode.Length >= 2 ? normalizedLanguageCode[..2] : normalizedLanguageCode;
    }
}
