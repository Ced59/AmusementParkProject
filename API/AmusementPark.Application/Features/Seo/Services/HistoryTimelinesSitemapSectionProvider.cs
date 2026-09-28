using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Seo.Services;

public sealed class HistoryTimelinesSitemapSectionProvider : ISitemapSectionProvider
{
    private const int PublicHistoryEventLimit = 50000;

    private readonly IHistoryEventRepository historyEventRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IStandaloneAttractionRepository? standaloneAttractionRepository;
    private readonly IParkZoneRepository? parkZoneRepository;
    private readonly IHistoricalKeyYearFactReader? historicalKeyYearFactReader;
    private readonly IHistoricalParkRolloutGateAssessmentService? rolloutGateAssessmentService;

    public HistoryTimelinesSitemapSectionProvider(
        IHistoryEventRepository historyEventRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IStandaloneAttractionRepository? standaloneAttractionRepository = null,
        IParkZoneRepository? parkZoneRepository = null,
        IHistoricalKeyYearFactReader? historicalKeyYearFactReader = null,
        IHistoricalParkRolloutGateAssessmentService? rolloutGateAssessmentService = null)
    {
        this.historyEventRepository = historyEventRepository;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.standaloneAttractionRepository = standaloneAttractionRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.historicalKeyYearFactReader = historicalKeyYearFactReader;
        this.rolloutGateAssessmentService = rolloutGateAssessmentService;
    }

    public string Key => SitemapSectionKeys.History;

    public string FileName => "history.xml";

    public string DisplayName => "Histoires";

    public async Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyCollection<HistoryEvent> events = await this.historyEventRepository.GetPublicVisibleEventsAsync(PublicHistoryEventLimit, cancellationToken);
        IReadOnlyCollection<Park> automaticParkCandidates = await LoadPublicHistoryParksAsync(this.parkRepository, cancellationToken);
        IReadOnlyCollection<ParkItem> automaticItemCandidates = await LoadPublicHistoryItemsAsync(this.parkItemRepository, cancellationToken);
        IReadOnlyCollection<StandaloneAttraction> automaticStandaloneCandidates = this.standaloneAttractionRepository is null
            ? Array.Empty<StandaloneAttraction>()
            : (await this.standaloneAttractionRepository.GetPublicSitemapCandidatesAsync(PublicHistoryEventLimit, cancellationToken))
                .Where(static attraction =>
                    HistorySitemapCandidateResolver.IsPublicHistoryStandaloneAttraction(attraction) &&
                    AutomaticHistoryEventFactory.HasLifecycleDate(attraction))
                .ToList();
        IReadOnlyCollection<HistoryEvent> automaticEvents = AutomaticHistoryEventFactory
            .CreateParkLifecycleEvents(automaticParkCandidates)
            .Concat(AutomaticHistoryEventFactory.CreateParkItemLifecycleEvents(automaticItemCandidates))
            .Concat(automaticStandaloneCandidates.SelectMany(AutomaticHistoryEventFactory.CreateStandaloneAttractionLifecycleEvents))
            .ToList();

        if (automaticEvents.Count > 0)
        {
            events = AutomaticHistoryEventFactory.MergeWithExplicitEvents(events, automaticEvents);
        }

        HistorySitemapResolvedData resolvedData = await HistorySitemapCandidateResolver.ResolveAsync(
            events,
            context.SupportedLanguages,
            this.parkRepository,
            this.parkItemRepository,
            cancellationToken,
            this.standaloneAttractionRepository);
        HistoryTimelineIndexability indexability = HistoryTimelineIndexabilityResolver.Resolve(resolvedData);
        IReadOnlyCollection<HistoricalKeyYearSitemapCandidate> keyYearCandidates =
            await this.LoadKeyYearSnapshotCandidatesAsync(
                automaticParkCandidates,
                automaticItemCandidates,
                cancellationToken);
        HashSet<string> rolloutOpenParkIds = keyYearCandidates
            .Select(static candidate => candidate.ParkId)
            .ToHashSet(StringComparer.Ordinal);

        Dictionary<string, SitemapUrlEntry> urlsByPath = new Dictionary<string, SitemapUrlEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (HistoryEvent historyEvent in resolvedData.Events)
        {
            if (historyEvent.EntityType == HistoryEntityType.Park)
            {
                if (indexability.ParkIds.Contains(historyEvent.OwnerId)
                    && rolloutOpenParkIds.Contains(historyEvent.OwnerId))
                {
                    AddParkTimelineUrls(urlsByPath, resolvedData, historyEvent);
                }

                continue;
            }

            if (historyEvent.EntityType == HistoryEntityType.StandaloneAttraction)
            {
                if (indexability.StandaloneAttractionIds.Contains(historyEvent.OwnerId))
                {
                    AddStandaloneAttractionTimelineUrls(urlsByPath, resolvedData, historyEvent);
                }

                continue;
            }

            string? parentParkId = HistoryTimelineIndexabilityResolver.ResolveParentParkId(resolvedData, historyEvent);
            if (parentParkId is not null
                && indexability.ParkIds.Contains(parentParkId)
                && rolloutOpenParkIds.Contains(parentParkId))
            {
                AddParentParkTimelineUrls(urlsByPath, resolvedData, historyEvent);
            }

            if (indexability.ParkItemIds.Contains(historyEvent.OwnerId))
            {
                AddParkItemTimelineUrls(urlsByPath, resolvedData, historyEvent);
            }
        }

        AddKeyYearSnapshotUrls(
            urlsByPath,
            resolvedData,
            keyYearCandidates);

        return urlsByPath.Values.OrderBy(static url => url.RelativePath, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<IReadOnlyCollection<HistoricalKeyYearSitemapCandidate>>
        LoadKeyYearSnapshotCandidatesAsync(
        IReadOnlyCollection<Park> parks,
        IReadOnlyCollection<ParkItem> parkItems,
        CancellationToken cancellationToken)
    {
        if (this.parkZoneRepository is null
            || this.historicalKeyYearFactReader is null
            || this.rolloutGateAssessmentService is null)
        {
            return Array.Empty<HistoricalKeyYearSitemapCandidate>();
        }

        IReadOnlyCollection<ParkZone> parkZones = await this.parkZoneRepository.GetAllAsync(cancellationToken);
        string[] parkIds = parks
            .Select(static park => park.Id)
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyCollection<HistoricalFact> facts =
            await this.historicalKeyYearFactReader.GetLatestDecisionEligibleRevisionsForParksAsync(
                parkIds,
                cancellationToken);
        return await HistoricalKeyYearSitemapCandidateResolver.ResolveAsync(
            parks,
            parkItems,
            parkZones,
            facts,
            this.rolloutGateAssessmentService,
            cancellationToken);
    }

    private static void AddKeyYearSnapshotUrls(
        Dictionary<string, SitemapUrlEntry> urlsByPath,
        HistorySitemapResolvedData resolvedData,
        IReadOnlyCollection<HistoricalKeyYearSitemapCandidate> candidates)
    {
        foreach (HistoricalKeyYearSitemapCandidate candidate in candidates)
        {
            string parkSlug = SeoSlugService.ToSlug(candidate.ParkName, "park");
            string timelinePathWithoutLanguage = $"park/{candidate.ParkId}/{parkSlug}/history";
            string snapshotPathWithoutLanguage = $"{timelinePathWithoutLanguage}/{candidate.Year}";
            foreach (string language in resolvedData.Languages)
            {
                HistorySitemapCandidateResolver.AddOrRefreshUrl(
                    urlsByPath,
                    $"/{language}/{timelinePathWithoutLanguage}",
                    candidate.LastModifiedAtUtc,
                    "monthly",
                    0.72m);
                HistorySitemapCandidateResolver.AddOrRefreshUrl(
                    urlsByPath,
                    $"/{language}/{snapshotPathWithoutLanguage}",
                    candidate.LastModifiedAtUtc,
                    "yearly",
                    0.68m);
            }
        }
    }

    private static async Task<IReadOnlyCollection<Park>> LoadPublicHistoryParksAsync(
        IParkRepository parkRepository,
        CancellationToken cancellationToken)
    {
        List<Park> parks = new List<Park>();
        int pageNumber = 1;

        while (true)
        {
            PagedResult<Park> page = await parkRepository.GetPageAsync(
                pageNumber,
                SitemapPublicCandidateLoader.PageSize,
                includeHidden: false,
                isVisible: true,
                adminReviewStatus: null,
                type: null,
                countryCode: null,
                hasValidCoordinates: null,
                closedFilter: ClosedEntityFilter.All,
                cancellationToken);

            parks.AddRange(page.Items.Where(static park =>
                HistorySitemapCandidateResolver.IsPublicHistoryPark(park)));

            if (page.Items.Count == 0 || page.Page >= page.TotalPages)
            {
                break;
            }

            pageNumber++;
        }

        return parks;
    }

    private static async Task<IReadOnlyCollection<ParkItem>> LoadPublicHistoryItemsAsync(
        IParkItemRepository parkItemRepository,
        CancellationToken cancellationToken)
    {
        List<ParkItem> items = new List<ParkItem>();
        int pageNumber = 1;

        while (true)
        {
            PagedResult<ParkItem> page = await parkItemRepository.GetPageAsync(
                pageNumber,
                SitemapPublicCandidateLoader.PageSize,
                parkId: null,
                search: null,
                includeHidden: false,
                isVisible: true,
                adminReviewStatus: null,
                category: null,
                type: null,
                zoneId: null,
                manufacturerId: null,
                contentBacklogFilter: null,
                cancellationToken: cancellationToken,
                sortField: ParkItemAdminSortField.ParkId);

            items.AddRange(page.Items.Where(static item =>
                HistorySitemapCandidateResolver.IsPublicHistoryItem(item)));

            if (page.Items.Count == 0 || page.Page >= page.TotalPages)
            {
                break;
            }

            pageNumber++;
        }

        return items;
    }

    private static void AddParkTimelineUrls(
        Dictionary<string, SitemapUrlEntry> urlsByPath,
        HistorySitemapResolvedData resolvedData,
        HistoryEvent historyEvent)
    {
        if (!resolvedData.PublicParkById.TryGetValue(historyEvent.OwnerId, out Park? park))
        {
            return;
        }

        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string pathWithoutLanguage = $"park/{park.Id}/{parkSlug}/history";
        foreach (string language in resolvedData.Languages)
        {
            HistorySitemapCandidateResolver.AddOrRefreshUrl(
                urlsByPath,
                $"/{language}/{pathWithoutLanguage}",
                historyEvent.UpdatedAtUtc,
                "monthly",
                0.72m);
        }
    }

    private static void AddStandaloneAttractionTimelineUrls(
        Dictionary<string, SitemapUrlEntry> urlsByPath,
        HistorySitemapResolvedData resolvedData,
        HistoryEvent historyEvent)
    {
        if (!resolvedData.PublicStandaloneAttractionById.TryGetValue(historyEvent.OwnerId, out StandaloneAttraction? attraction))
        {
            return;
        }

        string attractionSlug = SeoSlugService.ToSlug(attraction.Name, "attraction");
        string pathWithoutLanguage = $"attraction/{attraction.Id}/{attractionSlug}/history";
        foreach (string language in resolvedData.Languages)
        {
            HistorySitemapCandidateResolver.AddOrRefreshUrl(
                urlsByPath,
                $"/{language}/{pathWithoutLanguage}",
                historyEvent.UpdatedAtUtc,
                "monthly",
                0.70m);
        }
    }

    private static void AddParkItemTimelineUrls(
        Dictionary<string, SitemapUrlEntry> urlsByPath,
        HistorySitemapResolvedData resolvedData,
        HistoryEvent historyEvent)
    {
        if (!resolvedData.PublicItemById.TryGetValue(historyEvent.OwnerId, out ParkItem? item))
        {
            return;
        }

        if (!resolvedData.PublicParkById.TryGetValue(item.ParkId, out Park? itemPark))
        {
            return;
        }

        string parkSlug = SeoSlugService.ToSlug(itemPark.Name, "park");
        string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
        string pathWithoutLanguage = $"park/{itemPark.Id}/{parkSlug}/item/{item.Id}/{itemSlug}/history";
        foreach (string language in resolvedData.Languages)
        {
            HistorySitemapCandidateResolver.AddOrRefreshUrl(
                urlsByPath,
                $"/{language}/{pathWithoutLanguage}",
                historyEvent.UpdatedAtUtc,
                "monthly",
                0.70m);
        }
    }

    private static void AddParentParkTimelineUrls(
        Dictionary<string, SitemapUrlEntry> urlsByPath,
        HistorySitemapResolvedData resolvedData,
        HistoryEvent historyEvent)
    {
        if (!resolvedData.PublicItemById.TryGetValue(historyEvent.OwnerId, out ParkItem? item))
        {
            return;
        }

        string? parkId = HistorySitemapCandidateResolver.NormalizeId(historyEvent.ContextParkId)
            ?? HistorySitemapCandidateResolver.NormalizeId(historyEvent.ParkId)
            ?? item.ParkId;
        if (string.IsNullOrWhiteSpace(parkId) ||
            !resolvedData.PublicParkById.TryGetValue(parkId, out Park? park))
        {
            return;
        }

        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string pathWithoutLanguage = $"park/{park.Id}/{parkSlug}/history";
        foreach (string language in resolvedData.Languages)
        {
            HistorySitemapCandidateResolver.AddOrRefreshUrl(
                urlsByPath,
                $"/{language}/{pathWithoutLanguage}",
                historyEvent.UpdatedAtUtc,
                "monthly",
                0.71m);
        }
    }

}
