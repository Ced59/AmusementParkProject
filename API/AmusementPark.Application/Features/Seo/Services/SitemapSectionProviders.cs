using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
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
    private const int PublicSitemapCandidatePageSize = int.MaxValue;

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
        PagedResult<Park> page = await this.parkRepository.GetPageAsync(
            1,
            PublicSitemapCandidatePageSize,
            includeHidden: false,
            isVisible: true,
            adminReviewStatus: null,
            type: null,
            countryCode: null,
            hasValidCoordinates: null,
            cancellationToken);

        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();
        foreach (Park park in page.Items.Where(static park => IsPublicPark(park)))
        {
            string slug = SeoSlugService.ToSlug(park.Name, "park");
            foreach (string language in languages)
            {
                urls.Add(new SitemapUrlEntry($"/{language}/park/{park.Id}/{slug}", park.UpdatedAtUtc, "weekly", 0.85m));
                urls.Add(new SitemapUrlEntry($"/{language}/park/{park.Id}/{slug}/images", park.UpdatedAtUtc, "weekly", 0.72m));
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
}

/// <summary>
/// Provider des pages publiques d'éléments de parc.
/// </summary>
public sealed class ParkItemListsSitemapSectionProvider : ISitemapSectionProvider
{
    private const int PublicSitemapCandidateLimit = int.MaxValue;

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
        IReadOnlyCollection<string> parentParkIds = publicItems
            .Select(static item => item.ParkId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        IReadOnlyCollection<Park> parentParks = await this.parkRepository.GetByIdsAsync(parentParkIds, cancellationToken);
        Dictionary<string, Park> visibleParkById = parentParks
            .Where(static park => ParksSitemapSectionProvider.IsPublicPark(park))
            .ToDictionary(static park => park.Id!, static park => park, StringComparer.OrdinalIgnoreCase);

        Dictionary<string, DateTime?> lastModifiedByParkId = publicItems
            .Where(item => visibleParkById.ContainsKey(item.ParkId))
            .GroupBy(static item => item.ParkId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (DateTime?)group.Max(static item => item.UpdatedAtUtc),
                StringComparer.OrdinalIgnoreCase);

        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();
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
        IReadOnlyCollection<ParkItem> candidateItems = await parkItemRepository.GetPublicSitemapCandidatesAsync(
            PublicSitemapCandidateLimit,
            cancellationToken);

        return candidateItems
            .Where(static item => ParkItemsSitemapSectionProvider.IsPublicItem(item))
            .ToList();
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
            IReadOnlyCollection<ParkItem> publicParkItems = publicItems
                .Where(item => string.Equals(item.ParkId, parentPark.Id, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Dictionary<string, DateTime?> lastModifiedByZoneId = publicParkItems
                .Where(static item => !string.IsNullOrWhiteSpace(item.ZoneId))
                .GroupBy(static item => item.ZoneId!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    static group => group.Key,
                    static group => (DateTime?)group.Max(static item => item.UpdatedAtUtc),
                    StringComparer.OrdinalIgnoreCase);

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
            DateTime? zonesLastModifiedUtc = publicZones
                .Select(zone => ParkItemListsSitemapSectionProvider.ResolveLatest(zone.UpdatedAtUtc, lastModifiedByZoneId.GetValueOrDefault(zone.Id!)))
                .Concat(new DateTime?[] { parentPark.UpdatedAtUtc })
                .Where(static value => value.HasValue)
                .Max();

            foreach (string language in languages)
            {
                urls.Add(new SitemapUrlEntry($"/{language}/park/{parentPark.Id}/{parkSlug}/zones", zonesLastModifiedUtc, "weekly", 0.73m));
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
}

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

    internal static bool IsPublicItem(ParkItem item)
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
        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();

        IReadOnlyCollection<ParkOperator> operators = await this.parkOperatorRepository.GetAllAsync(cancellationToken);
        foreach (ParkOperator entity in operators.Where(static entity => entity.AdminReviewStatus != AdminReviewStatus.NotRelevant))
        {
            AddReferenceUrls(urls, languages, "park-operator", entity.Id, entity.Name, entity.UpdatedAtUtc);
        }

        IReadOnlyCollection<ParkFounder> founders = await this.parkFounderRepository.GetAllAsync(cancellationToken);
        foreach (ParkFounder entity in founders)
        {
            AddReferenceUrls(urls, languages, "park-founder", entity.Id, entity.Name, entity.UpdatedAtUtc);
        }

        IReadOnlyCollection<AttractionManufacturer> manufacturers = await this.attractionManufacturerRepository.GetAllAsync(cancellationToken);
        foreach (AttractionManufacturer entity in manufacturers.Where(static entity => entity.AdminReviewStatus != AdminReviewStatus.NotRelevant))
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
