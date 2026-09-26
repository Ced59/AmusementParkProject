using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class GetPublicParkHistoricalTimelineQueryHandler :
    IQueryHandler<GetPublicParkHistoricalTimelineQuery, ApplicationResult<PublicParkHistoricalTimelineResult>>
{
    private readonly PublicParkHistoricalDataLoader dataLoader;
    private readonly IHistoricalSourceRepository historicalSourceRepository;
    private readonly IHistoryEventRepository historyEventRepository;

    public GetPublicParkHistoricalTimelineQueryHandler(
        PublicParkHistoricalDataLoader dataLoader,
        IHistoricalSourceRepository historicalSourceRepository,
        IHistoryEventRepository historyEventRepository)
    {
        this.dataLoader = dataLoader;
        this.historicalSourceRepository = historicalSourceRepository;
        this.historyEventRepository = historyEventRepository;
    }

    public async Task<ApplicationResult<PublicParkHistoricalTimelineResult>> HandleAsync(
        GetPublicParkHistoricalTimelineQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<PublicParkHistoricalTimelineResult>.Failure(
                ApplicationErrors.Required("parkId"));
        }

        if (query.Page < 1
            || query.PageSize < 1
            || query.PageSize > GetPublicParkHistoricalTimelineQuery.MaximumPageSize)
        {
            return ApplicationResult<PublicParkHistoricalTimelineResult>.Failure(
                ApplicationErrors.InvalidPagination());
        }

        string parkId = query.ParkId.Trim();
        PublicParkHistoricalScope? scope = await this.dataLoader.LoadScopeAsync(parkId, cancellationToken);
        if (scope is null)
        {
            return ApplicationResult<PublicParkHistoricalTimelineResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(Park), parkId));
        }

        PagedResult<HistoricalFact> factPage = await this.dataLoader.GetTimelinePageAsync(
            scope,
            query.Page,
            query.PageSize,
            cancellationToken);
        HistoricalFact[] pageFacts = factPage.Items.ToArray();
        HistoricalSourceRevisionReference[] sourceReferences = pageFacts
            .SelectMany(static fact => fact.SourceReferences)
            .Distinct()
            .ToArray();
        IReadOnlyCollection<HistoricalSourceReference> loadedSources = sourceReferences.Length == 0
            ? Array.Empty<HistoricalSourceReference>()
            : await this.historicalSourceRepository.GetRevisionsAsync(
                sourceReferences,
                cancellationToken);
        Dictionary<(Guid Id, int Revision), HistoricalSourceReference> publicSources = loadedSources
            .Where(static source => source.PublicationState == HistoricalPublicationState.Published
                && source.Accessibility != HistoricalSourceAccessibility.Withdrawn)
            .ToDictionary(static source => (source.Id, source.Revision));
        string[] narrativeIds = pageFacts
            .Select(static fact => fact.NarrativeContentId)
            .Where(static narrativeId => !string.IsNullOrWhiteSpace(narrativeId))
            .Select(static narrativeId => narrativeId!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyCollection<HistoryEvent> loadedNarratives = narrativeIds.Length == 0
            ? Array.Empty<HistoryEvent>()
            : await this.historyEventRepository.GetPublishedArticlesByIdsAsync(
                narrativeIds,
                cancellationToken);
        Dictionary<string, HistoryEvent> publicNarratives = loadedNarratives.ToDictionary(
            static narrative => narrative.Id,
            StringComparer.Ordinal);
        Dictionary<(HistoricalSubjectType Type, string Id), HistoricalSubject> publicCurrentSubjects =
            scope.PublicCurrentSubjects.ToDictionary(static subject => (subject.Type, subject.Id));
        PublicHistoricalTimelineEntryResult[] entries = pageFacts
            .Select(fact => new PublicHistoricalTimelineEntryResult(
                fact,
                fact.SourceReferences
                    .Select(reference => publicSources.GetValueOrDefault(
                        (reference.SourceId, reference.Revision)))
                    .Where(static source => source is not null)
                    .Select(static source => source!)
                    .ToArray(),
                ResolvePublicNarrative(fact, publicNarratives, publicCurrentSubjects),
                ResolveCurrentSubjectName(fact, publicCurrentSubjects)))
            .ToArray();
        PagedResult<PublicHistoricalTimelineEntryResult> page = new(
            entries,
            query.Page,
            query.PageSize,
            factPage.TotalItems);
        IReadOnlyDictionary<string, string> publicZoneNames =
            PublicParkHistoricalDataLoader.ResolvePublicZoneNames(scope, pageFacts);

        return ApplicationResult<PublicParkHistoricalTimelineResult>.Success(
            new PublicParkHistoricalTimelineResult(scope.Park, page, publicZoneNames));
    }

    private static HistoryEvent? ResolvePublicNarrative(
        HistoricalFact fact,
        IReadOnlyDictionary<string, HistoryEvent> narratives,
        IReadOnlyDictionary<(HistoricalSubjectType Type, string Id), HistoricalSubject> publicCurrentSubjects)
    {
        if (fact.NarrativeContentId is null
            || !narratives.TryGetValue(fact.NarrativeContentId, out HistoryEvent? narrative)
            || !publicCurrentSubjects.ContainsKey((fact.Subject.Type, fact.Subject.Id)))
        {
            return null;
        }

        HistoryEntityType? expectedEntityType = fact.Subject.Type switch
        {
            HistoricalSubjectType.Park => HistoryEntityType.Park,
            HistoricalSubjectType.ParkItem => HistoryEntityType.ParkItem,
            _ => null,
        };
        return expectedEntityType == narrative.EntityType
            && string.Equals(narrative.OwnerId, fact.Subject.Id, StringComparison.Ordinal)
                ? narrative
                : null;
    }

    private static string? ResolveCurrentSubjectName(
        HistoricalFact fact,
        IReadOnlyDictionary<(HistoricalSubjectType Type, string Id), HistoricalSubject> publicCurrentSubjects)
    {
        return fact.Subject.Type == HistoricalSubjectType.ParkItem
            && publicCurrentSubjects.TryGetValue(
                (fact.Subject.Type, fact.Subject.Id),
                out HistoricalSubject? currentSubject)
                    ? currentSubject.HistoricalLabel
                    : null;
    }
}
