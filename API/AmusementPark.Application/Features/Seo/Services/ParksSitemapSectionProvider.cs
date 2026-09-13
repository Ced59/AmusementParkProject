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

/// <summary>
/// Provider des pages publiques de parcs.
/// </summary>
public sealed class ParksSitemapSectionProvider : ISitemapSectionProvider
{
    private const int PublicSitemapImagePageSize = 100;

    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public ParksSitemapSectionProvider(IParkRepository parkRepository, IParkItemRepository parkItemRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public string Key => SitemapSectionKeys.Parks;

    public string FileName => "parks.xml";

    public string DisplayName => "Parcs";

    public async Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyCollection<string> languages = NormalizeLanguages(context.SupportedLanguages);
        IReadOnlyCollection<Park> publicParks = await SitemapPublicCandidateLoader.LoadPublicParksAsync(
            this.parkRepository,
            cancellationToken);
        IReadOnlyCollection<ParkItem> publicItems = await SitemapPublicCandidateLoader.LoadPublicItemsAsync(
            this.parkItemRepository,
            cancellationToken);
        Dictionary<string, List<ParkItem>> mapItemsByParkId = publicItems
            .Where(HasPublicMapMarker)
            .GroupBy(static item => item.ParkId, StringComparer.OrdinalIgnoreCase)
            .Where(static group => SeoPageValuePolicy.IsCollectionIndexable(group.Count()))
            .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();
        foreach (Park park in publicParks)
        {
            string slug = SeoSlugService.ToSlug(park.Name, "park");
            foreach (string language in languages)
            {
                urls.Add(new SitemapUrlEntry($"/{language}/park/{park.Id}/{slug}", park.UpdatedAtUtc, "weekly", 0.85m));
                if (mapItemsByParkId.TryGetValue(park.Id!, out List<ParkItem>? mapItems) || park.HasPublicOfficialMaps())
                {
                    DateTime? mapLastModifiedUtc = ParkItemListsSitemapSectionProvider.ResolveLatest(
                        park.UpdatedAtUtc,
                        ParkItemListsSitemapSectionProvider.ResolveLatestParkItemUpdate(mapItems ?? new List<ParkItem>()));
                    urls.Add(new SitemapUrlEntry($"/{language}/park/{park.Id}/{slug}/map", mapLastModifiedUtc, "weekly", 0.78m));
                }

                if (HasWeatherCoordinates(park))
                {
                    urls.Add(new SitemapUrlEntry($"/{language}/park/{park.Id}/{slug}/weather", park.UpdatedAtUtc, "daily", 0.76m));
                }
            }
        }

        return urls;
    }

    internal static bool IsPublicPark(Park park)
    {
        return park.IsPubliclyDiscoverable();
    }

    internal static bool HasPublicMapMarker(ParkItem item)
    {
        return ParkItemsSitemapSectionProvider.IsPublicItem(item)
               && item.Position is not null
               && !(Math.Abs(item.Position.Latitude) < double.Epsilon && Math.Abs(item.Position.Longitude) < double.Epsilon);
    }

    internal static bool HasWeatherCoordinates(Park park)
    {
        return park.Status.IsOpenToVisitors()
               && park.Position is not null
               && !(Math.Abs(park.Position.Latitude) < double.Epsilon && Math.Abs(park.Position.Longitude) < double.Epsilon);
    }

    internal static IReadOnlyCollection<string> NormalizeLanguages(IReadOnlyCollection<string> languages)
    {
        List<string> normalizedLanguages = languages
            .Select(static language => language.Trim().ToLowerInvariant())
            .Where(static language => !string.IsNullOrWhiteSpace(language))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return normalizedLanguages.Count > 0 ? normalizedLanguages : new[] { "en" };
    }

    internal static async Task<IReadOnlyDictionary<string, PublishedImageOwnerSummary>> LoadPublishedImageOwnerSummariesAsync(
        IImageRepository imageRepository,
        ImageOwnerType ownerType,
        ImageCategory category,
        CancellationToken cancellationToken)
    {
        Dictionary<string, PublishedImageOwnerSummary> summaries = new Dictionary<string, PublishedImageOwnerSummary>(StringComparer.OrdinalIgnoreCase);
        int pageNumber = 1;

        while (true)
        {
            ImageSearchCriteria criteria = new ImageSearchCriteria(
                Category: category,
                OwnerType: ownerType,
                IsPublished: true,
                HasOwner: true);

            PagedResult<Image> page = await imageRepository.GetPageAsync(
                pageNumber,
                PublicSitemapImagePageSize,
                criteria,
                cancellationToken);

            foreach (Image image in page.Items)
            {
                string? ownerId = NormalizeOwnerId(image.OwnerId);
                if (ownerId is not null)
                {
                    PublishedImageOwnerSummary current = summaries.GetValueOrDefault(ownerId) ?? new PublishedImageOwnerSummary(0, null);
                    summaries[ownerId] = new PublishedImageOwnerSummary(
                        current.Count + 1,
                        ParkItemListsSitemapSectionProvider.ResolveLatest(current.LastModifiedUtc, image.UpdatedAtUtc));
                }
            }

            if (page.Items.Count == 0 || page.Page >= page.TotalPages)
            {
                break;
            }

            pageNumber++;
        }

        return summaries;
    }

    internal static async Task<HashSet<string>> LoadPublishedImageOwnerIdsAsync(
        IImageRepository imageRepository,
        ImageOwnerType ownerType,
        ImageCategory category,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, PublishedImageOwnerSummary> summaries = await LoadPublishedImageOwnerSummariesAsync(
            imageRepository,
            ownerType,
            category,
            cancellationToken);

        return summaries.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string? NormalizeOwnerId(string? ownerId)
    {
        return string.IsNullOrWhiteSpace(ownerId) ? null : ownerId.Trim();
    }

}
