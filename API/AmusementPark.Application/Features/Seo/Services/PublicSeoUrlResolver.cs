using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Seo.Services;

public sealed class PublicSeoUrlResolver
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
        IReadOnlyDictionary<string, List<PublicSeoParkItemSnapshot>> currentPublicItemsByParkId = await this.LoadCurrentPublicItemsByParkIdAsync(changedParkIds, cancellationToken);
        IReadOnlyDictionary<string, IReadOnlyCollection<ParkZone>> zonesByParkId = await this.LoadZonesByParkIdAsync(changedParkIds, cancellationToken);

        foreach (PublicSeoParkSnapshot park in parkSnapshots)
        {
            if (!IsPublicPark(park))
            {
                continue;
            }

            await this.AddParkImpactUrlsAsync(
                relativePaths,
                languages,
                park,
                currentPublicItemsByParkId.GetValueOrDefault(park.Id) ?? new List<PublicSeoParkItemSnapshot>(),
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
            .Where(IsPublicItem)
            .Select(static item => item.ParkId)
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        IReadOnlyDictionary<string, PublicSeoParkSnapshot> parentParkById = await this.LoadParentParksAsync(parentParkIds, update, cancellationToken);
        IReadOnlyDictionary<string, IReadOnlyCollection<ParkZone>> parentZonesByParkId = await this.LoadZonesByParkIdAsync(parentParkIds, cancellationToken);

        foreach (PublicSeoParkItemSnapshot item in itemSnapshots)
        {
            if (!parentParkById.TryGetValue(item.ParkId, out PublicSeoParkSnapshot? parentPark) || !IsPublicPark(parentPark))
            {
                continue;
            }

            if (!IsPublicItem(item))
            {
                continue;
            }

            AddParkDetailUrls(relativePaths, languages, parentPark);
            AddParkItemListUrls(relativePaths, languages, parentPark);

            IReadOnlyCollection<ParkZone> parentZones = parentZonesByParkId.GetValueOrDefault(item.ParkId) ?? Array.Empty<ParkZone>();
            AddZoneImpactUrls(relativePaths, languages, parentPark, new[] { item }, parentZones);

            AddParkItemDetailUrls(relativePaths, languages, parentPark, item);
            if (await this.HasPublishedImagesAsync(ImageOwnerType.ParkItem, ImageCategory.ParkItem, item.Id, imagePresenceByKey, cancellationToken))
            {
                AddParkItemImageUrls(relativePaths, languages, parentPark, item);
            }
        }

        return relativePaths.OrderBy(static path => path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task AddParkImpactUrlsAsync(
        HashSet<string> relativePaths,
        IReadOnlyCollection<string> languages,
        PublicSeoParkSnapshot park,
        IReadOnlyCollection<PublicSeoParkItemSnapshot> currentPublicItems,
        IReadOnlyCollection<ParkZone> zones,
        Dictionary<string, bool> imagePresenceByKey,
        CancellationToken cancellationToken)
    {
        AddParkDetailUrls(relativePaths, languages, park);

        if (await this.HasPublishedImagesAsync(ImageOwnerType.Park, ImageCategory.Park, park.Id, imagePresenceByKey, cancellationToken))
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
    }

    private async Task<IReadOnlyDictionary<string, List<PublicSeoParkItemSnapshot>>> LoadCurrentPublicItemsByParkIdAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken)
    {
        Dictionary<string, List<PublicSeoParkItemSnapshot>> itemsByParkId = new Dictionary<string, List<PublicSeoParkItemSnapshot>>(StringComparer.OrdinalIgnoreCase);
        foreach (string parkId in parkIds)
        {
            IReadOnlyCollection<ParkItem> items = await this.parkItemRepository.GetByParkIdAsync(parkId, true, cancellationToken);
            foreach (ParkItem item in items)
            {
                PublicSeoParkItemSnapshot? snapshot = PublicSeoParkItemSnapshot.FromParkItem(item);
                if (snapshot is null || !IsPublicItem(snapshot))
                {
                    continue;
                }

                if (!itemsByParkId.TryGetValue(snapshot.ParkId, out List<PublicSeoParkItemSnapshot>? parkItems))
                {
                    parkItems = new List<PublicSeoParkItemSnapshot>();
                    itemsByParkId[snapshot.ParkId] = parkItems;
                }

                parkItems.Add(snapshot);
            }
        }

        return itemsByParkId;
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

    private static void AddDiscoveryUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages)
    {
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/home");
            relativePaths.Add($"/{language}/parks");
        }
    }

    private static void AddParkDetailUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}");
        }
    }

    private static void AddParkImageUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/images");
        }
    }

    private static void AddParkItemListUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/items");
        }
    }

    private static void AddParkItemDetailUrls(
        HashSet<string> relativePaths,
        IReadOnlyCollection<string> languages,
        PublicSeoParkSnapshot park,
        PublicSeoParkItemSnapshot item)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/item/{item.Id}/{itemSlug}");
        }
    }

    private static void AddParkItemImageUrls(
        HashSet<string> relativePaths,
        IReadOnlyCollection<string> languages,
        PublicSeoParkSnapshot park,
        PublicSeoParkItemSnapshot item)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/item/{item.Id}/{itemSlug}/images");
        }
    }

    private static void AddZoneImpactUrls(
        HashSet<string> relativePaths,
        IReadOnlyCollection<string> languages,
        PublicSeoParkSnapshot park,
        IReadOnlyCollection<PublicSeoParkItemSnapshot> publicItems,
        IReadOnlyCollection<ParkZone> zones)
    {
        HashSet<string> impactedZoneIds = publicItems
            .Select(static item => item.ZoneId)
            .Where(static zoneId => !string.IsNullOrWhiteSpace(zoneId))
            .Select(static zoneId => zoneId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (impactedZoneIds.Count == 0)
        {
            return;
        }

        List<ParkZone> impactedZones = zones
            .Where(zone => !string.IsNullOrWhiteSpace(zone.Id) && impactedZoneIds.Contains(zone.Id) && ParkZonesSitemapSectionProvider.IsPublicZone(zone))
            .ToList();
        if (impactedZones.Count == 0)
        {
            return;
        }

        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/zones");
        }

        foreach (ParkZone zone in impactedZones)
        {
            string zoneSlug = SeoSlugService.ToSlug(zone.Name, "zone");
            foreach (string language in languages)
            {
                relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/zone/{zone.Id}/{zoneSlug}");
            }
        }
    }

    private static bool IsPublicPark(PublicSeoParkSnapshot park)
    {
        return !string.IsNullOrWhiteSpace(park.Id) &&
               !string.IsNullOrWhiteSpace(park.Name) &&
               park.IsVisible &&
               park.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    private static bool IsPublicItem(PublicSeoParkItemSnapshot item)
    {
        return !string.IsNullOrWhiteSpace(item.Id) &&
               !string.IsNullOrWhiteSpace(item.ParkId) &&
               !string.IsNullOrWhiteSpace(item.Name) &&
               item.IsVisible &&
               item.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    private static string BuildParkRouteKey(PublicSeoParkSnapshot park)
    {
        return $"{park.Id}:{SeoSlugService.ToSlug(park.Name, "park")}:{park.IsVisible}:{park.AdminReviewStatus}";
    }

    private static string BuildItemRouteKey(PublicSeoParkItemSnapshot item)
    {
        return $"{item.ParkId}:{item.Id}:{item.ZoneId}:{SeoSlugService.ToSlug(item.Name, "item")}:{item.IsVisible}:{item.AdminReviewStatus}";
    }
}
