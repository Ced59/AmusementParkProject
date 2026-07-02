using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Seo.Services;

public sealed partial class PublicSeoUrlResolver
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IImageRepository imageRepository;

    public PublicSeoUrlResolver(
        IParkItemRepository parkItemRepository,
        IParkRepository parkRepository,
        IParkZoneRepository parkZoneRepository,
        IImageRepository imageRepository)
    {
        this.parkItemRepository = parkItemRepository;
        this.parkRepository = parkRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.imageRepository = imageRepository;
    }

    public async Task<IReadOnlyCollection<string>> ResolveAsync(
        PublicSeoUpdate update,
        IReadOnlyCollection<string> supportedLanguages,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);

        IReadOnlyCollection<string> languages = ParksSitemapSectionProvider.NormalizeLanguages(supportedLanguages);
        HashSet<string> relativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, bool> imagePresenceByKey = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        IReadOnlyCollection<PublicSeoParkSnapshot> parkSnapshots = MergeParkSnapshots(update.PreviousParks, update.CurrentParks);
        IReadOnlyCollection<string> changedParkIds = parkSnapshots
            .Select(static park => park.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        PublicSeoParkItemsByParkId currentItemsByParkId = await this.LoadCurrentItemsByParkIdAsync(changedParkIds, cancellationToken);
        IReadOnlyDictionary<string, IReadOnlyCollection<ParkZone>> zonesByParkId = await this.LoadZonesByParkIdAsync(changedParkIds, cancellationToken);

        foreach (PublicSeoParkSnapshot park in parkSnapshots)
        {
            if (!IsPublicPark(park) && !IsPublicHistoryPark(park))
            {
                continue;
            }

            IReadOnlyCollection<PublicSeoParkItemSnapshot> currentPublicItems = currentItemsByParkId.PublicItemsByParkId.GetValueOrDefault(park.Id) ?? new List<PublicSeoParkItemSnapshot>();
            IReadOnlyCollection<PublicSeoParkItemSnapshot> currentHistoryItems = currentItemsByParkId.HistoryItemsByParkId.GetValueOrDefault(park.Id) ?? new List<PublicSeoParkItemSnapshot>();
            if (!IsPublicPark(park))
            {
                if (HasParkLifecycleDate(park) || currentHistoryItems.Any(HasParkItemLifecycleDate))
                {
                    AddParkHistoryUrls(relativePaths, languages, park);
                }

                foreach (PublicSeoParkItemSnapshot item in currentHistoryItems)
                {
                    if (HasParkItemLifecycleDate(item))
                    {
                        AddParkItemHistoryUrls(relativePaths, languages, park, item);
                    }
                }

                continue;
            }

            await this.AddParkImpactUrlsAsync(
                relativePaths,
                languages,
                park,
                currentPublicItems,
                currentHistoryItems,
                zonesByParkId.GetValueOrDefault(park.Id) ?? Array.Empty<ParkZone>(),
                imagePresenceByKey,
                cancellationToken);
        }

        if (update.IncludeDiscoveryPages && parkSnapshots.Any(IsPublicPark))
        {
            AddDiscoveryUrls(relativePaths, languages);
        }

        IReadOnlyCollection<PublicSeoParkItemSnapshot> itemSnapshots = MergeItemSnapshots(update.PreviousParkItems, update.CurrentParkItems);
        IReadOnlyCollection<string> parentParkIds = itemSnapshots
            .Where(static item => IsPublicItem(item) || IsPublicHistoryItem(item))
            .Select(static item => item.ParkId)
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        IReadOnlyDictionary<string, PublicSeoParkSnapshot> parentParkById = await this.LoadParentParksAsync(parentParkIds, update, cancellationToken);
        IReadOnlyDictionary<string, IReadOnlyCollection<ParkZone>> parentZonesByParkId = await this.LoadZonesByParkIdAsync(parentParkIds, cancellationToken);

        foreach (PublicSeoParkItemSnapshot item in itemSnapshots)
        {
            if (!parentParkById.TryGetValue(item.ParkId, out PublicSeoParkSnapshot? parentPark) || !IsPublicHistoryPark(parentPark))
            {
                continue;
            }

            bool isPublicItem = IsPublicItem(item);
            bool isPublicHistoryItem = IsPublicHistoryItem(item);
            if (!isPublicItem && !isPublicHistoryItem)
            {
                continue;
            }

            if (isPublicHistoryItem && HasParkItemLifecycleDate(item))
            {
                AddParkHistoryUrls(relativePaths, languages, parentPark);
                AddParkItemHistoryUrls(relativePaths, languages, parentPark, item);
            }

            if (!IsPublicPark(parentPark) || !isPublicItem)
            {
                continue;
            }

            AddParkDetailUrls(relativePaths, languages, parentPark);
            AddParkItemListUrls(relativePaths, languages, parentPark);
            if (HasPublicMapMarker(item))
            {
                AddParkMapUrls(relativePaths, languages, parentPark);
            }

            IReadOnlyCollection<ParkZone> parentZones = parentZonesByParkId.GetValueOrDefault(item.ParkId) ?? Array.Empty<ParkZone>();
            AddZoneImpactUrls(relativePaths, languages, parentPark, new[] { item }, parentZones);

            AddParkItemDetailUrls(relativePaths, languages, parentPark, item);
            bool itemHasImages = await this.HasPublishedImagesAsync(ImageOwnerType.ParkItem, ImageCategory.ParkItem, item.Id, imagePresenceByKey, cancellationToken);
            if (itemHasImages)
            {
                AddParkImageUrls(relativePaths, languages, parentPark);
                AddParkItemImageUrls(relativePaths, languages, parentPark, item);
            }
        }

        IReadOnlyCollection<PublicSeoVideoSnapshot> videoSnapshots = MergeVideoSnapshots(update.PreviousVideos, update.CurrentVideos);
        await this.AddVideoImpactUrlsAsync(relativePaths, languages, videoSnapshots, update, cancellationToken);

        return relativePaths.OrderBy(static path => path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task AddParkImpactUrlsAsync(
        HashSet<string> relativePaths,
        IReadOnlyCollection<string> languages,
        PublicSeoParkSnapshot park,
        IReadOnlyCollection<PublicSeoParkItemSnapshot> currentPublicItems,
        IReadOnlyCollection<PublicSeoParkItemSnapshot> currentHistoryItems,
        IReadOnlyCollection<ParkZone> zones,
        Dictionary<string, bool> imagePresenceByKey,
        CancellationToken cancellationToken)
    {
        AddParkDetailUrls(relativePaths, languages, park);

        if (currentPublicItems.Any(HasPublicMapMarker))
        {
            AddParkMapUrls(relativePaths, languages, park);
        }

        if (HasParkLifecycleDate(park) || currentHistoryItems.Any(HasParkItemLifecycleDate))
        {
            AddParkHistoryUrls(relativePaths, languages, park);
        }

        if (await this.HasPublishedParkOrItemImagesAsync(park.Id, currentPublicItems, imagePresenceByKey, cancellationToken))
        {
            AddParkImageUrls(relativePaths, languages, park);
        }

        if (currentPublicItems.Count > 0)
        {
            AddParkItemListUrls(relativePaths, languages, park);
        }

        AddZoneImpactUrls(relativePaths, languages, park, currentPublicItems, zones);

        foreach (PublicSeoParkItemSnapshot item in currentPublicItems)
        {
            AddParkItemDetailUrls(relativePaths, languages, park, item);
            if (await this.HasPublishedImagesAsync(ImageOwnerType.ParkItem, ImageCategory.ParkItem, item.Id, imagePresenceByKey, cancellationToken))
            {
                AddParkItemImageUrls(relativePaths, languages, park, item);
            }
        }

        foreach (PublicSeoParkItemSnapshot item in currentHistoryItems)
        {
            if (HasParkItemLifecycleDate(item))
            {
                AddParkItemHistoryUrls(relativePaths, languages, park, item);
            }
        }
    }

    private async Task<PublicSeoParkItemsByParkId> LoadCurrentItemsByParkIdAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken)
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

                if (IsPublicItem(snapshot))
                {
                    AddItemSnapshot(publicItemsByParkId, snapshot);
                }

                if (IsPublicHistoryItem(snapshot))
                {
                    AddItemSnapshot(historyItemsByParkId, snapshot);
                }
            }
        }

        return new PublicSeoParkItemsByParkId(publicItemsByParkId, historyItemsByParkId);
    }

    private static void AddItemSnapshot(
        IDictionary<string, List<PublicSeoParkItemSnapshot>> itemsByParkId,
        PublicSeoParkItemSnapshot snapshot)
    {
        if (!itemsByParkId.TryGetValue(snapshot.ParkId, out List<PublicSeoParkItemSnapshot>? parkItems))
        {
            parkItems = new List<PublicSeoParkItemSnapshot>();
            itemsByParkId[snapshot.ParkId] = parkItems;
        }

        parkItems.Add(snapshot);
    }

    private async Task AddVideoImpactUrlsAsync(
        HashSet<string> relativePaths,
        IReadOnlyCollection<string> languages,
        IReadOnlyCollection<PublicSeoVideoSnapshot> videoSnapshots,
        PublicSeoUpdate update,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<PublicSeoVideoSnapshot> publicVideos = videoSnapshots
            .Where(IsPublicVideo)
            .ToList();
        if (publicVideos.Count == 0)
        {
            return;
        }

        IReadOnlyCollection<PublicSeoVideoSnapshot> parkVideos = publicVideos
            .Where(static video => video.OwnerType == VideoOwnerType.Park)
            .ToList();
        IReadOnlyCollection<string> parkOwnerIds = parkVideos
            .Select(static video => video.OwnerId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        IReadOnlyDictionary<string, PublicSeoParkSnapshot> parksById = await this.LoadParentParksAsync(parkOwnerIds, update, cancellationToken);

        foreach (PublicSeoVideoSnapshot video in parkVideos)
        {
            if (!parksById.TryGetValue(video.OwnerId, out PublicSeoParkSnapshot? park) || !IsPublicPark(park))
            {
                continue;
            }

            AddParkDetailUrls(relativePaths, languages, park);
            IReadOnlyCollection<string> videoLanguages = ResolveVisibleLanguages(video, languages);
            AddParkVideoUrls(relativePaths, videoLanguages, park);
            AddParkVideoDetailUrls(relativePaths, videoLanguages, park, video);
        }

        IReadOnlyCollection<PublicSeoVideoSnapshot> itemVideos = publicVideos
            .Where(static video => video.OwnerType == VideoOwnerType.ParkItem)
            .ToList();
        IReadOnlyCollection<string> itemOwnerIds = itemVideos
            .Select(static video => video.OwnerId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        IReadOnlyDictionary<string, PublicSeoParkItemSnapshot> itemsById = await this.LoadVideoOwnerItemsAsync(itemOwnerIds, cancellationToken);
        IReadOnlyCollection<string> itemParkIds = itemsById.Values
            .Where(IsPublicItem)
            .Select(static item => item.ParkId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        IReadOnlyDictionary<string, PublicSeoParkSnapshot> itemParksById = await this.LoadParentParksAsync(itemParkIds, update, cancellationToken);

        foreach (PublicSeoVideoSnapshot video in itemVideos)
        {
            if (!itemsById.TryGetValue(video.OwnerId, out PublicSeoParkItemSnapshot? item) || !IsPublicItem(item))
            {
                continue;
            }

            if (!itemParksById.TryGetValue(item.ParkId, out PublicSeoParkSnapshot? park) || !IsPublicPark(park))
            {
                continue;
            }

            AddParkDetailUrls(relativePaths, languages, park);
            AddParkItemDetailUrls(relativePaths, languages, park, item);
            IReadOnlyCollection<string> videoLanguages = ResolveVisibleLanguages(video, languages);
            AddParkItemVideoUrls(relativePaths, videoLanguages, park, item);
            AddParkItemVideoDetailUrls(relativePaths, videoLanguages, park, item, video);
        }
    }

    private async Task<IReadOnlyDictionary<string, PublicSeoParkItemSnapshot>> LoadVideoOwnerItemsAsync(
        IReadOnlyCollection<string> itemIds,
        CancellationToken cancellationToken)
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

    private async Task<IReadOnlyDictionary<string, PublicSeoParkSnapshot>> LoadParentParksAsync(
        IReadOnlyCollection<string> parentParkIds,
        PublicSeoUpdate update,
        CancellationToken cancellationToken)
    {
        Dictionary<string, PublicSeoParkSnapshot> parentParkById = new Dictionary<string, PublicSeoParkSnapshot>(StringComparer.OrdinalIgnoreCase);
        foreach (PublicSeoParkSnapshot park in update.PreviousParks.Concat(update.CurrentParks))
        {
            if (!parentParkById.ContainsKey(park.Id))
            {
                parentParkById[park.Id] = park;
            }
        }

        IReadOnlyCollection<string> missingParkIds = parentParkIds
            .Where(parkId => !parentParkById.ContainsKey(parkId))
            .ToList();
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

    private async Task<IReadOnlyDictionary<string, IReadOnlyCollection<ParkZone>>> LoadZonesByParkIdAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken)
    {
        Dictionary<string, IReadOnlyCollection<ParkZone>> zonesByParkId = new Dictionary<string, IReadOnlyCollection<ParkZone>>(StringComparer.OrdinalIgnoreCase);
        foreach (string parkId in parkIds)
        {
            IReadOnlyCollection<ParkZone> zones = await this.parkZoneRepository.GetByParkIdAsync(parkId, cancellationToken);
            zonesByParkId[parkId] = zones;
        }

        return zonesByParkId;
    }

    private async Task<bool> HasPublishedImagesAsync(
        ImageOwnerType ownerType,
        ImageCategory category,
        string ownerId,
        Dictionary<string, bool> imagePresenceByKey,
        CancellationToken cancellationToken)
    {
        string key = $"{ownerType}:{category}:{ownerId}";
        if (imagePresenceByKey.TryGetValue(key, out bool cachedValue))
        {
            return cachedValue;
        }

        ImageSearchCriteria criteria = new ImageSearchCriteria(
            Category: category,
            OwnerType: ownerType,
            OwnerId: ownerId,
            IsPublished: true,
            HasOwner: true);
        PagedResult<Image> page = await this.imageRepository.GetPageAsync(1, 1, criteria, cancellationToken);
        bool hasImages = page.Items.Count > 0;
        imagePresenceByKey[key] = hasImages;
        return hasImages;
    }

    private async Task<bool> HasPublishedParkOrItemImagesAsync(
        string parkId,
        IReadOnlyCollection<PublicSeoParkItemSnapshot> currentPublicItems,
        Dictionary<string, bool> imagePresenceByKey,
        CancellationToken cancellationToken)
    {
        if (await this.HasPublishedImagesAsync(ImageOwnerType.Park, ImageCategory.Park, parkId, imagePresenceByKey, cancellationToken))
        {
            return true;
        }

        foreach (PublicSeoParkItemSnapshot item in currentPublicItems)
        {
            if (await this.HasPublishedImagesAsync(ImageOwnerType.ParkItem, ImageCategory.ParkItem, item.Id, imagePresenceByKey, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyCollection<PublicSeoParkSnapshot> MergeParkSnapshots(
        IReadOnlyCollection<PublicSeoParkSnapshot> previousParks,
        IReadOnlyCollection<PublicSeoParkSnapshot> currentParks)
    {
        return previousParks
            .Concat(currentParks)
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
            .GroupBy(static park => BuildParkRouteKey(park), StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .ToList();
    }

    private static IReadOnlyCollection<PublicSeoParkItemSnapshot> MergeItemSnapshots(
        IReadOnlyCollection<PublicSeoParkItemSnapshot> previousItems,
        IReadOnlyCollection<PublicSeoParkItemSnapshot> currentItems)
    {
        return previousItems
            .Concat(currentItems)
            .Where(static item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.ParkId))
            .GroupBy(static item => BuildItemRouteKey(item), StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .ToList();
    }

    private static IReadOnlyCollection<PublicSeoVideoSnapshot> MergeVideoSnapshots(
        IReadOnlyCollection<PublicSeoVideoSnapshot> previousVideos,
        IReadOnlyCollection<PublicSeoVideoSnapshot> currentVideos)
    {
        return previousVideos
            .Concat(currentVideos)
            .Where(static video => !string.IsNullOrWhiteSpace(video.Id) && !string.IsNullOrWhiteSpace(video.OwnerId))
            .GroupBy(static video => BuildVideoRouteKey(video), StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .ToList();
    }

    private static bool IsPublicPark(PublicSeoParkSnapshot park)
    {
        return !string.IsNullOrWhiteSpace(park.Id) &&
               !string.IsNullOrWhiteSpace(park.Name) &&
               park.IsVisible &&
               park.Status != ParkStatus.ClosedDefinitively &&
               park.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    private static bool IsPublicItem(PublicSeoParkItemSnapshot item)
    {
        return !string.IsNullOrWhiteSpace(item.Id) &&
               !string.IsNullOrWhiteSpace(item.ParkId) &&
               !string.IsNullOrWhiteSpace(item.Name) &&
               item.IsVisible &&
               !ParkItemStatusNormalizer.IsClosedDefinitively(item.Status) &&
               item.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    private static bool IsPublicHistoryPark(PublicSeoParkSnapshot park)
    {
        return !string.IsNullOrWhiteSpace(park.Id) &&
               !string.IsNullOrWhiteSpace(park.Name) &&
               park.IsVisible &&
               park.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    private static bool IsPublicHistoryItem(PublicSeoParkItemSnapshot item)
    {
        return !string.IsNullOrWhiteSpace(item.Id) &&
               !string.IsNullOrWhiteSpace(item.ParkId) &&
               !string.IsNullOrWhiteSpace(item.Name) &&
               item.IsVisible &&
               item.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    private static bool IsPublicVideo(PublicSeoVideoSnapshot video)
    {
        return !string.IsNullOrWhiteSpace(video.Id) &&
               !string.IsNullOrWhiteSpace(video.OwnerId) &&
               (video.OwnerType == VideoOwnerType.Park || video.OwnerType == VideoOwnerType.ParkItem) &&
               video.IsPublished;
    }

    private static bool HasParkLifecycleDate(PublicSeoParkSnapshot park)
    {
        return park.OpeningDate.HasValue ||
               park.ClosingDate.HasValue ||
               AutomaticHistoryEventFactory.HasLifecycleDateText(park.OpeningDateText) ||
               AutomaticHistoryEventFactory.HasLifecycleDateText(park.ClosingDateText);
    }

    private static bool HasParkItemLifecycleDate(PublicSeoParkItemSnapshot item)
    {
        return item.OpeningDate.HasValue ||
               item.ClosingDate.HasValue ||
               AutomaticHistoryEventFactory.HasLifecycleDateText(item.OpeningDateText) ||
               AutomaticHistoryEventFactory.HasLifecycleDateText(item.ClosingDateText);
    }

    private static bool HasPublicMapMarker(PublicSeoParkItemSnapshot item)
    {
        return IsPublicItem(item) && item.HasPosition;
    }

    private static string BuildParkRouteKey(PublicSeoParkSnapshot park)
    {
        return $"{park.Id}:{SeoSlugService.ToSlug(park.Name, "park")}:{park.IsVisible}:{park.Status}:{park.AdminReviewStatus}:{park.OpeningDate?.Ticks}:{park.ClosingDate?.Ticks}:{park.OpeningDateText}:{park.ClosingDateText}";
    }

    private static string BuildItemRouteKey(PublicSeoParkItemSnapshot item)
    {
        return $"{item.ParkId}:{item.Id}:{item.ZoneId}:{SeoSlugService.ToSlug(item.Name, "item")}:{item.IsVisible}:{item.Status}:{item.AdminReviewStatus}:{item.OpeningDate?.Ticks}:{item.ClosingDate?.Ticks}:{item.OpeningDateText}:{item.ClosingDateText}:{item.HasPosition}";
    }

    private static string BuildVideoRouteKey(PublicSeoVideoSnapshot video)
    {
        return $"{video.OwnerType}:{video.OwnerId}:{video.Id}:{SeoSlugService.ToSlug(video.Title, "video")}:{video.IsPublished}:{string.Join(",", NormalizeVideoLanguageCodes(video.LanguageCodes))}";
    }

    private static IReadOnlyCollection<string> ResolveVisibleLanguages(PublicSeoVideoSnapshot video, IReadOnlyCollection<string> languages)
    {
        IReadOnlyCollection<string> videoLanguageCodes = NormalizeVideoLanguageCodes(video.LanguageCodes);
        if (videoLanguageCodes.Count == 0)
        {
            return languages;
        }

        return languages
            .Where(language => videoLanguageCodes.Contains(NormalizeLanguageCode(language), StringComparer.Ordinal))
            .ToList();
    }

    private static IReadOnlyCollection<string> NormalizeVideoLanguageCodes(IReadOnlyCollection<string> languageCodes)
    {
        return languageCodes
            .Where(static languageCode => !string.IsNullOrWhiteSpace(languageCode))
            .Select(static languageCode => NormalizeLanguageCode(languageCode))
            .Where(static languageCode => languageCode.Length == 2)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static string NormalizeLanguageCode(string languageCode)
    {
        string normalizedLanguageCode = languageCode.Trim().ToLowerInvariant();
        return normalizedLanguageCode.Length >= 2 ? normalizedLanguageCode[..2] : normalizedLanguageCode;
    }

    private sealed record PublicSeoParkItemsByParkId(
        IReadOnlyDictionary<string, List<PublicSeoParkItemSnapshot>> PublicItemsByParkId,
        IReadOnlyDictionary<string, List<PublicSeoParkItemSnapshot>> HistoryItemsByParkId);
}
