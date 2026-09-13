using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.TechnicalPages.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.TechnicalPages;

namespace AmusementPark.Application.Features.Seo.Services;

public sealed class ParkItemListsSitemapSectionProvider : ISitemapSectionProvider
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public ParkItemListsSitemapSectionProvider(IParkRepository parkRepository, IParkItemRepository parkItemRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public string Key => SitemapSectionKeys.ParkItemLists;

    public string FileName => "park-item-lists.xml";

    public string DisplayName => "Listes d'elements de parc";

    public async Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyCollection<string> languages = ParksSitemapSectionProvider.NormalizeLanguages(context.SupportedLanguages);
        IReadOnlyCollection<ParkItem> publicItems = await LoadPublicItemsAsync(this.parkItemRepository, cancellationToken);
        IReadOnlyDictionary<string, List<ParkItem>> publicItemsByParkId = GroupItemsByParkId(publicItems);
        IReadOnlyCollection<string> parentParkIds = publicItemsByParkId
            .Where(static pair => SeoPageValuePolicy.IsCollectionIndexable(pair.Value.Count))
            .Select(static pair => pair.Key)
            .ToList();

        IReadOnlyCollection<Park> parentParks = await this.parkRepository.GetByIdsAsync(parentParkIds, cancellationToken);
        Dictionary<string, Park> visibleParkById = parentParks
            .Where(static park => ParksSitemapSectionProvider.IsPublicPark(park))
            .ToDictionary(static park => park.Id!, static park => park, StringComparer.OrdinalIgnoreCase);

        Dictionary<string, DateTime?> lastModifiedByParkId = new Dictionary<string, DateTime?>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, List<ParkItem>> parkItems in publicItemsByParkId)
        {
            if (!visibleParkById.ContainsKey(parkItems.Key) || !SeoPageValuePolicy.IsCollectionIndexable(parkItems.Value.Count))
            {
                continue;
            }

            lastModifiedByParkId[parkItems.Key] = ResolveLatestParkItemUpdate(parkItems.Value);
        }

        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>(visibleParkById.Count * languages.Count);
        foreach (Park parentPark in visibleParkById.Values.OrderBy(static park => park.Name, StringComparer.OrdinalIgnoreCase))
        {
            string parkSlug = SeoSlugService.ToSlug(parentPark.Name, "park");
            DateTime? lastModifiedUtc = ResolveLatest(parentPark.UpdatedAtUtc, lastModifiedByParkId.GetValueOrDefault(parentPark.Id!));
            foreach (string language in languages)
            {
                urls.Add(new SitemapUrlEntry($"/{language}/park/{parentPark.Id}/{parkSlug}/items", lastModifiedUtc, "weekly", 0.74m));
            }
        }

        return urls;
    }

    internal static async Task<IReadOnlyCollection<ParkItem>> LoadPublicItemsAsync(IParkItemRepository parkItemRepository, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkItem> publicItems = await SitemapPublicCandidateLoader.LoadPublicItemsAsync(
            parkItemRepository,
            cancellationToken);

        return publicItems;
    }

    internal static IReadOnlyDictionary<string, List<ParkItem>> GroupItemsByParkId(IReadOnlyCollection<ParkItem> items)
    {
        Dictionary<string, List<ParkItem>> groupedItems = new Dictionary<string, List<ParkItem>>(StringComparer.OrdinalIgnoreCase);
        foreach (ParkItem item in items)
        {
            if (!groupedItems.TryGetValue(item.ParkId, out List<ParkItem>? parkItems))
            {
                parkItems = new List<ParkItem>();
                groupedItems[item.ParkId] = parkItems;
            }

            parkItems.Add(item);
        }

        return groupedItems;
    }

    internal static DateTime? ResolveLatestParkItemUpdate(IReadOnlyCollection<ParkItem> items)
    {
        DateTime? latest = null;
        foreach (ParkItem item in items)
        {
            if (!latest.HasValue || item.UpdatedAtUtc > latest.Value)
            {
                latest = item.UpdatedAtUtc;
            }
        }

        return latest;
    }

    internal static DateTime? ResolveLatest(DateTime? first, DateTime? second)
    {
        if (!first.HasValue)
        {
            return second;
        }

        if (!second.HasValue)
        {
            return first;
        }

        return first.Value > second.Value ? first : second;
    }
}
