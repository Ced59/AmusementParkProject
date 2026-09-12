using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Seo.Services;

/// <summary>
/// Provider des pages publiques d'images d'elements de parc.
/// </summary>
public sealed class ParkItemImagesSitemapSectionProvider : ISitemapSectionProvider
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IImageRepository imageRepository;

    public ParkItemImagesSitemapSectionProvider(IParkRepository parkRepository, IParkItemRepository parkItemRepository, IImageRepository imageRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.imageRepository = imageRepository;
    }

    public string Key => SitemapSectionKeys.ParkItemImages;

    public string FileName => "park-item-images.xml";

    public string DisplayName => "Images d'elements de parc";

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
        IReadOnlyDictionary<string, ParksSitemapSectionProvider.PublishedImageOwnerSummary> itemImageSummaries = await ParksSitemapSectionProvider.LoadPublishedImageOwnerSummariesAsync(
            this.imageRepository,
            ImageOwnerType.ParkItem,
            ImageCategory.ParkItem,
            cancellationToken);

        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();
        foreach (ParkItem item in publicItems)
        {
            if (!visibleParkById.TryGetValue(item.ParkId, out Park? parentPark) ||
                !itemImageSummaries.TryGetValue(item.Id!, out ParksSitemapSectionProvider.PublishedImageOwnerSummary? imageSummary) ||
                !SeoPageValuePolicy.IsImageGalleryIndexable(imageSummary.Count))
            {
                continue;
            }

            string parkSlug = parkSlugById[item.ParkId];
            string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
            foreach (string language in languages)
            {
                DateTime? lastModifiedUtc = ParkItemListsSitemapSectionProvider.ResolveLatest(item.UpdatedAtUtc, imageSummary.LastModifiedUtc);
                urls.Add(new SitemapUrlEntry($"/{language}/park/{parentPark.Id}/{parkSlug}/item/{item.Id}/{itemSlug}/images", lastModifiedUtc, "weekly", 0.62m));
            }
        }

        return urls;
    }
}
