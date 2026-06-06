using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Seo.Services;

/// <summary>
/// Provider des pages publiques statiques.
/// </summary>
public sealed class StaticPagesSitemapSectionProvider : ISitemapSectionProvider
{
    private static readonly IReadOnlyCollection<StaticSitemapPage> StaticPages = new[]
    {
        new StaticSitemapPage("home", "daily", 1.0m),
        new StaticSitemapPage("parks", "daily", 0.9m),
        new StaticSitemapPage("about", "monthly", 0.4m),
        new StaticSitemapPage("privacy", "yearly", 0.2m),
    };

    public string Key => SitemapSectionKeys.Static;

    public string FileName => "static.xml";

    public string DisplayName => "Pages statiques";

    public Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();
        foreach (string language in NormalizeLanguages(context.SupportedLanguages))
        {
            foreach (StaticSitemapPage page in StaticPages)
            {
                urls.Add(new SitemapUrlEntry($"/{language}/{page.Segment}", null, page.ChangeFrequency, page.Priority));
            }
        }

        return Task.FromResult<IReadOnlyCollection<SitemapUrlEntry>>(urls);
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

    private sealed record StaticSitemapPage(string Segment, string ChangeFrequency, decimal Priority);
}

/// <summary>
/// Provider des pages publiques de parcs.
/// </summary>
public sealed class ParksSitemapSectionProvider : ISitemapSectionProvider
{
    private readonly IParkRepository parkRepository;

    public ParksSitemapSectionProvider(IParkRepository parkRepository)
    {
        this.parkRepository = parkRepository;
    }

    public string Key => SitemapSectionKeys.Parks;

    public string FileName => "parks.xml";

    public string DisplayName => "Parcs";

    public async Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyCollection<string> languages = NormalizeLanguages(context.SupportedLanguages);
        int limit = NormalizeDynamicLimit(context.MaxDynamicUrlsPerType);
        PagedResult<Park> page = await this.parkRepository.GetPageAsync(
            1,
            limit,
            includeHidden: false,
            isVisible: true,
            adminReviewStatus: null,
            type: null,
            countryCode: null,
            cancellationToken);

        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();
        foreach (Park park in page.Items.Where(static park => IsPublicPark(park)))
        {
            string slug = SeoSlugService.ToSlug(park.Name, "park");
            foreach (string language in languages)
            {
                urls.Add(new SitemapUrlEntry($"/{language}/park/{park.Id}/{slug}", park.UpdatedAtUtc, "weekly", 0.85m));
                urls.Add(new SitemapUrlEntry($"/{language}/park/{park.Id}/{slug}/items", park.UpdatedAtUtc, "weekly", 0.75m));
            }
        }

        return urls;
    }

    internal static bool IsPublicPark(Park park)
    {
        return !string.IsNullOrWhiteSpace(park.Id) &&
               !string.IsNullOrWhiteSpace(park.Name) &&
               park.IsVisible &&
               park.AdminReviewStatus != AdminReviewStatus.NotRelevant;
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

    internal static int NormalizeDynamicLimit(int value)
    {
        return Math.Clamp(value, 1, 50000);
    }
}

/// <summary>
/// Provider des pages publiques d'éléments de parc.
/// </summary>
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
        int limit = ParksSitemapSectionProvider.NormalizeDynamicLimit(context.MaxDynamicUrlsPerType);
        IReadOnlyCollection<ParkItem> candidateItems = await this.parkItemRepository.GetPublicSitemapCandidatesAsync(
            limit,
            cancellationToken);

        IReadOnlyCollection<ParkItem> publicItems = candidateItems
            .Where(static item => IsPublicItem(item))
            .ToList();

        IReadOnlyCollection<string> parentParkIds = publicItems
            .Select(static item => item.ParkId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        IReadOnlyCollection<Park> parentParks = await this.parkRepository.GetByIdsAsync(parentParkIds, cancellationToken);
        Dictionary<string, Park> visibleParkById = parentParks
            .Where(static park => ParksSitemapSectionProvider.IsPublicPark(park))
            .ToDictionary(static park => park.Id!, static park => park, StringComparer.OrdinalIgnoreCase);

        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();
        foreach (ParkItem item in publicItems)
        {
            if (!visibleParkById.TryGetValue(item.ParkId, out Park? parentPark))
            {
                continue;
            }

            string parkSlug = SeoSlugService.ToSlug(parentPark.Name, "park");
            string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
            foreach (string language in languages)
            {
                urls.Add(new SitemapUrlEntry($"/{language}/park/{parentPark.Id}/{parkSlug}/item/{item.Id}/{itemSlug}", item.UpdatedAtUtc, "weekly", 0.7m));
            }
        }

        return urls;
    }

    private static bool IsPublicItem(ParkItem item)
    {
        return !string.IsNullOrWhiteSpace(item.Id) &&
               !string.IsNullOrWhiteSpace(item.ParkId) &&
               !string.IsNullOrWhiteSpace(item.Name) &&
               item.IsVisible &&
               item.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }
}

/// <summary>
/// Provider des références publiques : exploitants, fondateurs et constructeurs.
/// </summary>
public sealed class ReferencesSitemapSectionProvider : ISitemapSectionProvider
{
    private readonly IParkOperatorRepository parkOperatorRepository;
    private readonly IParkFounderRepository parkFounderRepository;
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;

    public ReferencesSitemapSectionProvider(
        IParkOperatorRepository parkOperatorRepository,
        IParkFounderRepository parkFounderRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository)
    {
        this.parkOperatorRepository = parkOperatorRepository;
        this.parkFounderRepository = parkFounderRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
    }

    public string Key => SitemapSectionKeys.References;

    public string FileName => "references.xml";

    public string DisplayName => "Références";

    public async Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyCollection<string> languages = ParksSitemapSectionProvider.NormalizeLanguages(context.SupportedLanguages);
        int limit = Math.Min(ParksSitemapSectionProvider.NormalizeDynamicLimit(context.MaxDynamicUrlsPerType), 5000);
        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();

        IReadOnlyCollection<ParkOperator> operators = await this.parkOperatorRepository.GetAllAsync(cancellationToken);
        foreach (ParkOperator entity in operators.Where(static entity => entity.AdminReviewStatus != AdminReviewStatus.NotRelevant).Take(limit))
        {
            AddReferenceUrls(urls, languages, "park-operator", entity.Id, entity.Name, entity.UpdatedAtUtc);
        }

        IReadOnlyCollection<ParkFounder> founders = await this.parkFounderRepository.GetAllAsync(cancellationToken);
        foreach (ParkFounder entity in founders.Take(limit))
        {
            AddReferenceUrls(urls, languages, "park-founder", entity.Id, entity.Name, entity.UpdatedAtUtc);
        }

        IReadOnlyCollection<AttractionManufacturer> manufacturers = await this.attractionManufacturerRepository.GetAllAsync(cancellationToken);
        foreach (AttractionManufacturer entity in manufacturers.Where(static entity => entity.AdminReviewStatus != AdminReviewStatus.NotRelevant).Take(limit))
        {
            AddReferenceUrls(urls, languages, "park-manufacturer", entity.Id, entity.Name, entity.UpdatedAtUtc);
        }

        return urls;
    }

    private static void AddReferenceUrls(List<SitemapUrlEntry> urls, IReadOnlyCollection<string> languages, string routeSegment, string? id, string? name, DateTime? lastModifiedUtc)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        string slug = SeoSlugService.ToSlug(name, "reference");
        foreach (string language in languages)
        {
            urls.Add(new SitemapUrlEntry($"/{language}/{routeSegment}/{id}/{slug}", lastModifiedUtc, "monthly", 0.55m));
        }
    }
}
