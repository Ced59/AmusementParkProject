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
/// Provider des pages publiques d'images de parcs.
/// </summary>
public sealed class ParkImagesSitemapSectionProvider : ISitemapSectionProvider
{
    private readonly IParkRepository parkRepository;
    private readonly IImageRepository imageRepository;

    public ParkImagesSitemapSectionProvider(IParkRepository parkRepository, IImageRepository imageRepository)
    {
        this.parkRepository = parkRepository;
        this.imageRepository = imageRepository;
    }

    public string Key => SitemapSectionKeys.ParkImages;

    public string FileName => "park-images.xml";

    public string DisplayName => "Images de parcs";

    public async Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyCollection<string> languages = ParksSitemapSectionProvider.NormalizeLanguages(context.SupportedLanguages);
        IReadOnlyCollection<Park> publicParks = await SitemapPublicCandidateLoader.LoadPublicParksAsync(
            this.parkRepository,
            cancellationToken);

        HashSet<string> parkIdsWithPublishedImages = await ParksSitemapSectionProvider.LoadPublishedImageOwnerIdsAsync(
            this.imageRepository,
            ImageOwnerType.Park,
            ImageCategory.Park,
            cancellationToken);

        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();
        foreach (Park park in publicParks)
        {
            if (!parkIdsWithPublishedImages.Contains(park.Id!))
            {
                continue;
            }

            string slug = SeoSlugService.ToSlug(park.Name, "park");
            foreach (string language in languages)
            {
                urls.Add(new SitemapUrlEntry($"/{language}/park/{park.Id}/{slug}/images", park.UpdatedAtUtc, "weekly", 0.72m));
            }
        }

        return urls;
    }
}

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
        HashSet<string> itemIdsWithPublishedImages = await ParksSitemapSectionProvider.LoadPublishedImageOwnerIdsAsync(
            this.imageRepository,
            ImageOwnerType.ParkItem,
            ImageCategory.ParkItem,
            cancellationToken);

        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();
        foreach (ParkItem item in publicItems)
        {
            if (!visibleParkById.TryGetValue(item.ParkId, out Park? parentPark) || !itemIdsWithPublishedImages.Contains(item.Id!))
            {
                continue;
            }

            string parkSlug = parkSlugById[item.ParkId];
            string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
            foreach (string language in languages)
            {
                urls.Add(new SitemapUrlEntry($"/{language}/park/{parentPark.Id}/{parkSlug}/item/{item.Id}/{itemSlug}/images", item.UpdatedAtUtc, "weekly", 0.62m));
            }
        }

        return urls;
    }
}
