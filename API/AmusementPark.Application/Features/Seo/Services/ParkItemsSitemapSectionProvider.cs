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

public sealed class ParkItemsSitemapSectionProvider : ISitemapSectionProvider
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public ParkItemsSitemapSectionProvider(IParkRepository parkRepository, IParkItemRepository parkItemRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public string Key => SitemapSectionKeys.ParkItems;

    public string FileName => "park-items.xml";

    public string DisplayName => "Éléments de parc";

    public async Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyCollection<string> languages = ParksSitemapSectionProvider.NormalizeLanguages(context.SupportedLanguages);
        IReadOnlyCollection<ParkItem> publicItems = await ParkItemListsSitemapSectionProvider.LoadPublicItemsAsync(this.parkItemRepository, cancellationToken);
        IReadOnlyDictionary<string, List<ParkItem>> publicItemsByParkId = ParkItemListsSitemapSectionProvider.GroupItemsByParkId(publicItems);

        IReadOnlyCollection<string> parentParkIds = publicItemsByParkId.Keys.ToList();

        IReadOnlyCollection<Park> parentParks = await this.parkRepository.GetByIdsAsync(parentParkIds, cancellationToken);
        Dictionary<string, Park> visibleParkById = parentParks
            .Where(static park => ParksSitemapSectionProvider.IsPublicPark(park))
            .ToDictionary(static park => park.Id!, static park => park, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> parkSlugById = visibleParkById.ToDictionary(
            static pair => pair.Key,
            static pair => SeoSlugService.ToSlug(pair.Value.Name, "park"),
            StringComparer.OrdinalIgnoreCase);
        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>(publicItems.Count * languages.Count);
        foreach (ParkItem item in publicItems)
        {
            if (!visibleParkById.TryGetValue(item.ParkId, out Park? parentPark))
            {
                continue;
            }

            string parkSlug = parkSlugById[item.ParkId];
            string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
            foreach (string language in languages)
            {
                urls.Add(new SitemapUrlEntry($"/{language}/park/{parentPark.Id}/{parkSlug}/item/{item.Id}/{itemSlug}", item.UpdatedAtUtc, "weekly", 0.7m));
            }
        }

        return urls;
    }

    internal static bool IsPublicItem(ParkItem item)
    {
        return !string.IsNullOrWhiteSpace(item.Id) &&
               !string.IsNullOrWhiteSpace(item.ParkId) &&
               !string.IsNullOrWhiteSpace(item.Name) &&
               item.IsVisible &&
               !ParkItemStatusNormalizer.IsClosedDefinitively(item.AttractionDetails?.Status) &&
               item.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }
}
