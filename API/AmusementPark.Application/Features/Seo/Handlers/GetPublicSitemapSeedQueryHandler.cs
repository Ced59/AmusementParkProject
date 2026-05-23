using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Seo.Handlers;

/// <summary>
/// Construit un sitemap seed à partir des entités publiques visibles.
/// </summary>
public sealed class GetPublicSitemapSeedQueryHandler : IQueryHandler<GetPublicSitemapSeedQuery, ApplicationResult<IReadOnlyCollection<PublicSitemapUrl>>>
{
    private static readonly IReadOnlyCollection<string> StaticPublicPages = new[]
    {
        "home",
        "parks",
        "about",
        "privacy",
    };

    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkOperatorRepository parkOperatorRepository;
    private readonly IParkFounderRepository parkFounderRepository;
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;

    public GetPublicSitemapSeedQueryHandler(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IParkOperatorRepository parkOperatorRepository,
        IParkFounderRepository parkFounderRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkOperatorRepository = parkOperatorRepository;
        this.parkFounderRepository = parkFounderRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<PublicSitemapUrl>>> HandleAsync(GetPublicSitemapSeedQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> languages = NormalizeLanguages(query.SupportedLanguages);
        int dynamicLimit = Math.Clamp(query.MaxDynamicUrlsPerType, 1, 5000);
        List<PublicSitemapUrl> urls = new List<PublicSitemapUrl>();

        this.AddStaticPages(urls, languages);
        IReadOnlyCollection<Park> visibleParks = await this.GetVisibleParksAsync(dynamicLimit, cancellationToken);
        Dictionary<string, Park> visibleParkById = visibleParks
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
            .ToDictionary(static park => park.Id!, static park => park, StringComparer.OrdinalIgnoreCase);

        this.AddParkUrls(urls, visibleParks, languages);
        await this.AddParkItemUrlsAsync(urls, visibleParkById, languages, dynamicLimit, cancellationToken);

        int referenceLimit = Math.Min(dynamicLimit, 25);
        await this.AddReferenceUrlsAsync(urls, languages, referenceLimit, cancellationToken);

        IReadOnlyCollection<PublicSitemapUrl> distinctUrls = urls
            .GroupBy(static url => url.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.OrderByDescending(url => url.LastModifiedUtc).First())
            .OrderBy(static url => url.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return ApplicationResult<IReadOnlyCollection<PublicSitemapUrl>>.Success(distinctUrls);
    }

    private static IReadOnlyCollection<string> NormalizeLanguages(IReadOnlyCollection<string> languages)
    {
        List<string> normalizedLanguages = languages
            .Select(static language => language.Trim().ToLowerInvariant())
            .Where(static language => !string.IsNullOrWhiteSpace(language))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return normalizedLanguages.Count > 0 ? normalizedLanguages : new[] { "en" };
    }

    private void AddStaticPages(List<PublicSitemapUrl> urls, IReadOnlyCollection<string> languages)
    {
        foreach (string language in languages)
        {
            foreach (string page in StaticPublicPages)
            {
                urls.Add(new PublicSitemapUrl($"/{language}/{page}", null));
            }
        }
    }

    private async Task<IReadOnlyCollection<Park>> GetVisibleParksAsync(int dynamicLimit, CancellationToken cancellationToken)
    {
        PagedResult<Park> page = await this.parkRepository.GetPageAsync(
            1,
            dynamicLimit,
            includeHidden: false,
            isVisible: true,
            adminReviewStatus: null,
            type: null,
            countryCode: null,
            cancellationToken);

        return page.Items
            .Where(static park => park.IsVisible && park.AdminReviewStatus != AdminReviewStatus.NotRelevant)
            .ToList();
    }

    private void AddParkUrls(List<PublicSitemapUrl> urls, IReadOnlyCollection<Park> parks, IReadOnlyCollection<string> languages)
    {
        foreach (Park park in parks)
        {
            if (string.IsNullOrWhiteSpace(park.Id))
            {
                continue;
            }

            string slug = SeoSlugService.ToSlug(park.Name, "park");
            foreach (string language in languages)
            {
                urls.Add(new PublicSitemapUrl($"/{language}/park/{park.Id}/{slug}", park.UpdatedAtUtc));
                urls.Add(new PublicSitemapUrl($"/{language}/park/{park.Id}/{slug}/items", park.UpdatedAtUtc));
            }
        }
    }

    private async Task AddParkItemUrlsAsync(List<PublicSitemapUrl> urls, IReadOnlyDictionary<string, Park> visibleParkById, IReadOnlyCollection<string> languages, int dynamicLimit, CancellationToken cancellationToken)
    {
        PagedResult<ParkItem> page = await this.parkItemRepository.GetPageAsync(
            1,
            dynamicLimit,
            parkId: null,
            search: null,
            includeHidden: false,
            isVisible: true,
            adminReviewStatus: null,
            category: null,
            type: null,
            manufacturerId: null,
            cancellationToken);

        foreach (ParkItem item in page.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Id) || !item.IsVisible || item.AdminReviewStatus == AdminReviewStatus.NotRelevant)
            {
                continue;
            }

            if (!visibleParkById.TryGetValue(item.ParkId, out Park? parentPark) || string.IsNullOrWhiteSpace(parentPark.Id))
            {
                continue;
            }

            string parkSlug = SeoSlugService.ToSlug(parentPark.Name, "park");
            string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
            foreach (string language in languages)
            {
                urls.Add(new PublicSitemapUrl($"/{language}/park/{parentPark.Id}/{parkSlug}/item/{item.Id}/{itemSlug}", item.UpdatedAtUtc));
            }
        }
    }

    private async Task AddReferenceUrlsAsync(List<PublicSitemapUrl> urls, IReadOnlyCollection<string> languages, int dynamicLimit, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkOperator> operators = await this.parkOperatorRepository.GetAllAsync(cancellationToken);
        IReadOnlyCollection<ParkFounder> founders = await this.parkFounderRepository.GetAllAsync(cancellationToken);
        IReadOnlyCollection<AttractionManufacturer> manufacturers = await this.attractionManufacturerRepository.GetAllAsync(cancellationToken);

        foreach (ParkOperator entity in operators.Where(static entity => entity.AdminReviewStatus != AdminReviewStatus.NotRelevant).Take(dynamicLimit))
        {
            this.AddReferenceUrls(urls, languages, "park-operator", entity.Id, entity.Name, entity.UpdatedAtUtc);
        }

        foreach (ParkFounder entity in founders.Take(dynamicLimit))
        {
            this.AddReferenceUrls(urls, languages, "park-founder", entity.Id, entity.Name, entity.UpdatedAtUtc);
        }

        foreach (AttractionManufacturer entity in manufacturers.Where(static entity => entity.AdminReviewStatus != AdminReviewStatus.NotRelevant).Take(dynamicLimit))
        {
            this.AddReferenceUrls(urls, languages, "park-manufacturer", entity.Id, entity.Name, entity.UpdatedAtUtc);
        }
    }

    private void AddReferenceUrls(List<PublicSitemapUrl> urls, IReadOnlyCollection<string> languages, string routeSegment, string? id, string? name, DateTime? lastModifiedUtc)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        string slug = SeoSlugService.ToSlug(name, "reference");
        foreach (string language in languages)
        {
            urls.Add(new PublicSitemapUrl($"/{language}/{routeSegment}/{id}/{slug}", lastModifiedUtc));
        }
    }
}
