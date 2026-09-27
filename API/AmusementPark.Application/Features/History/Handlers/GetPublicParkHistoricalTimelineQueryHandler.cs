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
    private const int MaximumLineageRelations = 200;
    private const int MaximumLineageSubjectsPerBatch = 20;

    private readonly PublicParkHistoricalDataLoader dataLoader;
    private readonly IHistoricalSourceRepository historicalSourceRepository;
    private readonly IHistoryEventRepository historyEventRepository;
    private readonly IHistoricalRelationRepository? historicalRelationRepository;
    private readonly IHistoricalSubjectPublicationStateReader? subjectPublicationStateReader;

    public GetPublicParkHistoricalTimelineQueryHandler(
        PublicParkHistoricalDataLoader dataLoader,
        IHistoricalSourceRepository historicalSourceRepository,
        IHistoryEventRepository historyEventRepository,
        IHistoricalRelationRepository? historicalRelationRepository = null,
        IHistoricalSubjectPublicationStateReader? subjectPublicationStateReader = null)
    {
        this.dataLoader = dataLoader;
        this.historicalSourceRepository = historicalSourceRepository;
        this.historyEventRepository = historyEventRepository;
        this.historicalRelationRepository = historicalRelationRepository;
        this.subjectPublicationStateReader = subjectPublicationStateReader;
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
        IReadOnlyCollection<HistoricalSourceReference> latestTimelineSources = loadedSources.Count == 0
            ? Array.Empty<HistoricalSourceReference>()
            : await this.historicalSourceRepository.GetLatestRevisionsAsync(
                loadedSources.Select(static source => source.Id).Distinct().ToArray(),
                cancellationToken);
        Dictionary<(Guid Id, int Revision), HistoricalSourceReference> publicSources =
            HistoricalRelationEvidenceValidator.FilterCurrentlyAdmissiblePublicSources(
                loadedSources,
                latestTimelineSources)
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
        HashSet<HistoricalSubjectKey> subjectsWithLineage = await this.LoadSubjectsWithLineageAsync(
            pageFacts,
            cancellationToken);
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
                ResolveCurrentSubjectName(fact, publicCurrentSubjects),
                subjectsWithLineage.Contains(new HistoricalSubjectKey(
                    fact.Subject.Type,
                    fact.Subject.Id,
                    fact.Subject.ContextParkId))))
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

    private async Task<HashSet<HistoricalSubjectKey>> LoadSubjectsWithLineageAsync(
        IReadOnlyCollection<HistoricalFact> facts,
        CancellationToken cancellationToken)
    {
        if (this.historicalRelationRepository is null
            || this.subjectPublicationStateReader is null
            || facts.Count == 0)
        {
            return new HashSet<HistoricalSubjectKey>();
        }

        HistoricalSubjectKey[] keys = facts
            .Select(static fact => new HistoricalSubjectKey(
                fact.Subject.Type,
                fact.Subject.Id,
                fact.Subject.ContextParkId))
            .Distinct()
            .ToArray();
        Dictionary<Guid, HistoricalRelation> relationsById = new();
        foreach (HistoricalSubjectKey[] keyBatch in keys.Chunk(MaximumLineageSubjectsPerBatch))
        {
            IReadOnlyCollection<HistoricalRelation> batchRelations =
                await this.historicalRelationRepository
                    .GetLatestDecisionEligibleRevisionsTouchingEachSubjectAsync(
                        keyBatch,
                        MaximumLineageRelations,
                        cancellationToken);
            foreach (HistoricalRelation relation in batchRelations)
            {
                relationsById[relation.Id] = relation;
            }
        }

        HistoricalRelation[] relations = relationsById.Values.ToArray();
        HistoricalSubject[] relationSubjects = relations
            .SelectMany(static relation => new[] { relation.Source, relation.Target })
            .DistinctBy(static subject => (subject.Type, subject.Id, subject.ContextParkId))
            .ToArray();
        IReadOnlySet<HistoricalSubjectKey> publicCurrentKeys =
            await this.subjectPublicationStateReader.GetPublicSubjectKeysAsync(
                relationSubjects,
                cancellationToken);
        HistoricalRelationSourceRevisionReference[] references = relations
            .SelectMany(static relation => relation.SourceReferences)
            .DistinctBy(static reference => (reference.SourceId, reference.Revision))
            .ToArray();
        IReadOnlyCollection<HistoricalSourceReference> sources = references.Length == 0
            ? Array.Empty<HistoricalSourceReference>()
            : await this.historicalSourceRepository.GetRevisionsAsync(references, cancellationToken);
        IReadOnlyCollection<HistoricalSourceReference> latestSources = sources.Count == 0
            ? Array.Empty<HistoricalSourceReference>()
            : await this.historicalSourceRepository.GetLatestRevisionsAsync(
                sources.Select(static source => source.Id).Distinct().ToArray(),
                cancellationToken);
        IReadOnlyCollection<HistoricalSourceReference> currentlyPublicSources =
            HistoricalRelationEvidenceValidator.FilterCurrentlyAdmissiblePublicSources(
                sources,
                latestSources);
        return relations
            .Where(relation => IsPublic(relation.Source, publicCurrentKeys)
                && IsPublic(relation.Target, publicCurrentKeys)
                && HistoricalRelationEvidenceValidator.HasAdmissiblePublicSupport(
                    relation,
                    currentlyPublicSources))
            .SelectMany(static relation => new[]
            {
                new HistoricalSubjectKey(
                    relation.Source.Type,
                    relation.Source.Id,
                    relation.Source.ContextParkId),
                new HistoricalSubjectKey(
                    relation.Target.Type,
                    relation.Target.Id,
                    relation.Target.ContextParkId),
            })
            .Where(keys.Contains)
            .ToHashSet();
    }

    private static bool IsPublic(
        HistoricalSubject subject,
        IReadOnlySet<HistoricalSubjectKey> publicCurrentKeys)
    {
        return subject.PublicationPolicy switch
        {
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject => publicCurrentKeys.Contains(
                new HistoricalSubjectKey(subject.Type, subject.Id, subject.ContextParkId)),
            HistoricalSubjectPublicationPolicy.HistoricalOnly => subject.Type is not HistoricalSubjectType.ParkItem
                    and not HistoricalSubjectType.ParkZone
                || publicCurrentKeys.Contains(new HistoricalSubjectKey(
                    subject.Type,
                    subject.Id,
                    subject.ContextParkId)),
            _ => false,
        };
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
