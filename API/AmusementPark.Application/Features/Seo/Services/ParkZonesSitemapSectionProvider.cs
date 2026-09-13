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

public sealed class ParkZonesSitemapSectionProvider : ISitemapSectionProvider
{
    private readonly IParkRepository parkRepository;
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IParkItemRepository parkItemRepository;

    public ParkZonesSitemapSectionProvider(
        IParkRepository parkRepository,
        IParkZoneRepository parkZoneRepository,
        IParkItemRepository parkItemRepository)
    {
        this.parkRepository = parkRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public string Key => SitemapSectionKeys.ParkZones;

    public string FileName => "park-zones.xml";

    public string DisplayName => "Zones de parc";

    public async Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyCollection<string> languages = ParksSitemapSectionProvider.NormalizeLanguages(context.SupportedLanguages);
        IReadOnlyCollection<ParkItem> publicItems = await ParkItemListsSitemapSectionProvider.LoadPublicItemsAsync(this.parkItemRepository, cancellationToken);
        IReadOnlyDictionary<string, List<ParkItem>> publicItemsByParkId = ParkItemListsSitemapSectionProvider.GroupItemsByParkId(publicItems);
        IReadOnlyCollection<string> parentParkIds = publicItems
            .Where(static item => !string.IsNullOrWhiteSpace(item.ZoneId))
            .Select(static item => item.ParkId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        IReadOnlyCollection<Park> parentParks = await this.parkRepository.GetByIdsAsync(parentParkIds, cancellationToken);
        Dictionary<string, Park> visibleParkById = parentParks
            .Where(static park => ParksSitemapSectionProvider.IsPublicPark(park))
            .ToDictionary(static park => park.Id!, static park => park, StringComparer.OrdinalIgnoreCase);

        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();
        foreach (Park parentPark in visibleParkById.Values.OrderBy(static park => park.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (!publicItemsByParkId.TryGetValue(parentPark.Id!, out List<ParkItem>? publicParkItems))
            {
                continue;
            }

            Dictionary<string, DateTime?> lastModifiedByZoneId = BuildLastModifiedByZoneId(publicParkItems);

            IReadOnlyCollection<ParkZone> zones = await this.parkZoneRepository.GetByParkIdAsync(parentPark.Id!, cancellationToken);
            IReadOnlyCollection<ParkZone> publicZones = zones
                .Where(zone => IsPublicZone(zone) && lastModifiedByZoneId.ContainsKey(zone.Id!))
                .OrderBy(static zone => zone.SortOrder)
                .ThenBy(static zone => zone.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (publicZones.Count == 0)
            {
                continue;
            }

            string parkSlug = SeoSlugService.ToSlug(parentPark.Name, "park");
            DateTime? zonesLastModifiedUtc = parentPark.UpdatedAtUtc;
            foreach (ParkZone zone in publicZones)
            {
                zonesLastModifiedUtc = ParkItemListsSitemapSectionProvider.ResolveLatest(
                    zonesLastModifiedUtc,
                    ParkItemListsSitemapSectionProvider.ResolveLatest(zone.UpdatedAtUtc, lastModifiedByZoneId.GetValueOrDefault(zone.Id!)));
            }

            if (SeoPageValuePolicy.IsCollectionIndexable(publicZones.Count))
            {
                foreach (string language in languages)
                {
                    urls.Add(new SitemapUrlEntry($"/{language}/park/{parentPark.Id}/{parkSlug}/zones", zonesLastModifiedUtc, "weekly", 0.73m));
                }
            }

            foreach (ParkZone zone in publicZones)
            {
                string zoneSlug = SeoSlugService.ToSlug(zone.Name, "zone");
                DateTime? zoneLastModifiedUtc = ParkItemListsSitemapSectionProvider.ResolveLatest(zone.UpdatedAtUtc, lastModifiedByZoneId.GetValueOrDefault(zone.Id!));
                foreach (string language in languages)
                {
                    urls.Add(new SitemapUrlEntry($"/{language}/park/{parentPark.Id}/{parkSlug}/zone/{zone.Id}/{zoneSlug}", zoneLastModifiedUtc, "weekly", 0.71m));
                }
            }
        }

        return urls;
    }

    internal static bool IsPublicZone(ParkZone zone)
    {
        return !string.IsNullOrWhiteSpace(zone.Id) &&
               !string.IsNullOrWhiteSpace(zone.ParkId) &&
               !string.IsNullOrWhiteSpace(zone.Name) &&
               zone.IsVisible;
    }

    private static Dictionary<string, DateTime?> BuildLastModifiedByZoneId(IReadOnlyCollection<ParkItem> publicParkItems)
    {
        Dictionary<string, DateTime?> lastModifiedByZoneId = new Dictionary<string, DateTime?>(StringComparer.OrdinalIgnoreCase);
        foreach (ParkItem item in publicParkItems)
        {
            if (string.IsNullOrWhiteSpace(item.ZoneId))
            {
                continue;
            }

            if (!lastModifiedByZoneId.TryGetValue(item.ZoneId, out DateTime? current) || !current.HasValue || item.UpdatedAtUtc > current.Value)
            {
                lastModifiedByZoneId[item.ZoneId] = item.UpdatedAtUtc;
            }
        }

        return lastModifiedByZoneId;
    }
}
