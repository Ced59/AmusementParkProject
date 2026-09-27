using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class GetPublicHistoricalLineageQueryHandler :
    IQueryHandler<GetPublicHistoricalLineageQuery, ApplicationResult<PublicHistoricalLineageResult>>
{
    public const int MaximumDepth = 4;
    public const int MaximumSubjectCount = 60;
    private const int MaximumRelationsPerLevel = 200;

    private readonly IHistoricalRelationRepository relationRepository;
    private readonly IHistoricalSourceRepository sourceRepository;
    private readonly IHistoricalSubjectPublicationStateReader subjectPublicationStateReader;

    public GetPublicHistoricalLineageQueryHandler(
        IHistoricalRelationRepository relationRepository,
        IHistoricalSourceRepository sourceRepository,
        IHistoricalSubjectPublicationStateReader subjectPublicationStateReader)
    {
        this.relationRepository = relationRepository;
        this.sourceRepository = sourceRepository;
        this.subjectPublicationStateReader = subjectPublicationStateReader;
    }

    public async Task<ApplicationResult<PublicHistoricalLineageResult>> HandleAsync(
        GetPublicHistoricalLineageQuery query,
        CancellationToken cancellationToken = default)
    {
        string normalizedSubjectId = query.SubjectId?.Trim() ?? string.Empty;
        string? normalizedContextParkId = string.IsNullOrWhiteSpace(query.ContextParkId)
            ? null
            : query.ContextParkId.Trim();
        if (!Enum.IsDefined(query.SubjectType) || normalizedSubjectId.Length == 0)
        {
            return ApplicationResult<PublicHistoricalLineageResult>.Failure(
                ApplicationErrors.Required("historicalSubject"));
        }

        if (normalizedSubjectId.Length > 200
            || normalizedSubjectId.Any(char.IsControl)
            || normalizedContextParkId is not null
                && (normalizedContextParkId.Length > 200 || normalizedContextParkId.Any(char.IsControl))
            || query.SubjectType is HistoricalSubjectType.ParkItem or HistoricalSubjectType.ParkZone
                && normalizedContextParkId is null)
        {
            return ApplicationResult<PublicHistoricalLineageResult>.Failure(
                ApplicationError.Validation(
                    "history.subject.invalid",
                    "Le sujet historique demandé est invalide."));
        }

        string? rootContextParkId = query.SubjectType == HistoricalSubjectType.Park
            ? normalizedSubjectId
            : normalizedContextParkId;
        HistoricalSubjectKey rootKey = new(
            query.SubjectType,
            normalizedSubjectId,
            rootContextParkId);
        Dictionary<Guid, HistoricalRelation> loadedRelations = new();
        HashSet<HistoricalSubjectKey> visited = new() { rootKey };
        HashSet<HistoricalSubjectKey> frontier = new() { rootKey };
        bool isTruncated = false;
        for (int depth = 0; depth < MaximumDepth && frontier.Count > 0; depth++)
        {
            IReadOnlyCollection<HistoricalRelation> level =
                await this.relationRepository.GetLatestDecisionEligibleRevisionsTouchingSubjectsAsync(
                    frontier,
                    MaximumRelationsPerLevel,
                    cancellationToken);
            isTruncated |= level.Count >= MaximumRelationsPerLevel;
            HashSet<HistoricalSubjectKey> next = new();
            foreach (HistoricalRelation relation in level.OrderBy(static relation => relation.Id))
            {
                HistoricalSubjectKey sourceKey = ToKey(relation.Source);
                HistoricalSubjectKey targetKey = ToKey(relation.Target);
                if (!frontier.Contains(sourceKey) && !frontier.Contains(targetKey))
                {
                    continue;
                }

                HistoricalSubjectKey[] newKeys = new[] { sourceKey, targetKey }
                    .Distinct()
                    .Where(key => !visited.Contains(key))
                    .ToArray();
                if (visited.Count + newKeys.Length > MaximumSubjectCount)
                {
                    isTruncated = true;
                    continue;
                }

                loadedRelations[relation.Id] = relation;
                AddNextSubject(sourceKey, visited, next);
                AddNextSubject(targetKey, visited, next);
            }

            frontier = next;
        }

        if (frontier.Count > 0 && !isTruncated)
        {
            IReadOnlyCollection<HistoricalRelation> beyondMaximumDepth =
                await this.relationRepository.GetLatestDecisionEligibleRevisionsTouchingSubjectsAsync(
                    frontier,
                    MaximumRelationsPerLevel,
                    cancellationToken);
            isTruncated = beyondMaximumDepth.Any(relation =>
                !visited.Contains(ToKey(relation.Source)) || !visited.Contains(ToKey(relation.Target)));
        }

        if (loadedRelations.Count == 0)
        {
            return ApplicationResult<PublicHistoricalLineageResult>.Failure(
                ApplicationErrors.EntityNotFound("historical-lineage", rootKey.Id));
        }

        HistoricalSubject[] loadedSubjects = loadedRelations.Values
            .SelectMany(static relation => new[] { relation.Source, relation.Target })
            .DistinctBy(static subject => (subject.Type, subject.Id, subject.ContextParkId))
            .ToArray();
        IReadOnlySet<HistoricalSubjectKey> publicCurrentKeys =
            await this.subjectPublicationStateReader.GetPublicSubjectKeysAsync(
                loadedSubjects,
                cancellationToken);
        HistoricalRelation[] visibleRelations = loadedRelations.Values
            .Where(relation => IsPublic(relation.Source, publicCurrentKeys)
                && IsPublic(relation.Target, publicCurrentKeys))
            .ToArray();
        HistoricalRelation[] subjectVisibleConnectedRelations = SelectConnectedRelations(rootKey, visibleRelations);
        HistoricalRelationSourceRevisionReference[] references = subjectVisibleConnectedRelations
            .SelectMany(static relation => relation.SourceReferences)
            .DistinctBy(static reference => (reference.SourceId, reference.Revision))
            .ToArray();
        IReadOnlyCollection<HistoricalSourceReference> loadedSources = references.Length == 0
            ? Array.Empty<HistoricalSourceReference>()
            : await this.sourceRepository.GetRevisionsAsync(references, cancellationToken);
        IReadOnlyCollection<HistoricalSourceReference> latestSources = loadedSources.Count == 0
            ? Array.Empty<HistoricalSourceReference>()
            : await this.sourceRepository.GetLatestRevisionsAsync(
                loadedSources.Select(static source => source.Id).Distinct().ToArray(),
                cancellationToken);
        IReadOnlyCollection<HistoricalSourceReference> currentlyPublicSources =
            HistoricalRelationEvidenceValidator.FilterCurrentlyAdmissiblePublicSources(
                loadedSources,
                latestSources);
        Dictionary<(Guid Id, int Revision), HistoricalSourceReference> publicSources = currentlyPublicSources
            .ToDictionary(static source => (source.Id, source.Revision));
        HistoricalRelation[] evidenceBackedRelations = subjectVisibleConnectedRelations
            .Where(relation => HistoricalRelationEvidenceValidator.HasAdmissiblePublicSupport(
                relation,
                currentlyPublicSources))
            .ToArray();
        HistoricalRelation[] connectedRelations = SelectConnectedRelations(rootKey, evidenceBackedRelations);
        HistoricalSubject? root = connectedRelations
            .SelectMany(static relation => new[] { relation.Source, relation.Target })
            .FirstOrDefault(subject => ToKey(subject) == rootKey);
        if (root is null)
        {
            return ApplicationResult<PublicHistoricalLineageResult>.Failure(
                ApplicationErrors.EntityNotFound("historical-lineage", rootKey.Id));
        }

        PublicHistoricalLineageContextParkResult? contextPark = await this.ResolveContextParkAsync(
            root,
            cancellationToken);

        PublicHistoricalRelationResult[] relationResults = connectedRelations
            .OrderBy(static relation => relation.Period.Start?.Year ?? int.MinValue)
            .ThenBy(static relation => relation.Type)
            .ThenBy(static relation => relation.Id)
            .Select(relation => new PublicHistoricalRelationResult(
                relation,
                relation.SourceReferences.Select(reference => publicSources.GetValueOrDefault(
                        (reference.SourceId, reference.Revision)))
                    .Where(static source => source is not null)
                    .Select(static source => source!)
                    .ToArray()))
            .ToArray();
        HistoricalSubject[] subjects = connectedRelations
            .SelectMany(static relation => new[] { relation.Source, relation.Target })
            .DistinctBy(static subject => (subject.Type, subject.Id, subject.ContextParkId))
            .OrderBy(static subject => subject.HistoricalLabel, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static subject => subject.Type)
            .ToArray();

        return ApplicationResult<PublicHistoricalLineageResult>.Success(new PublicHistoricalLineageResult(
            root,
            contextPark,
            subjects,
            relationResults,
            HistoricalLineageCycleDetector.HasDirectedCycle(connectedRelations),
            isTruncated,
            MaximumDepth));
    }

    private async Task<PublicHistoricalLineageContextParkResult?> ResolveContextParkAsync(
        HistoricalSubject root,
        CancellationToken cancellationToken)
    {
        string? parkId = root.Type == HistoricalSubjectType.Park
            ? root.Id
            : root.ContextParkId;
        if (string.IsNullOrWhiteSpace(parkId))
        {
            return null;
        }

        IReadOnlyDictionary<string, string> publicParkNames =
            await this.subjectPublicationStateReader.GetPublicParkNamesAsync(
                new[] { parkId },
                cancellationToken);
        return publicParkNames.TryGetValue(parkId, out string? parkName)
            ? new PublicHistoricalLineageContextParkResult(parkId, parkName)
            : null;
    }

    private static void AddNextSubject(
        HistoricalSubjectKey key,
        ISet<HistoricalSubjectKey> visited,
        ISet<HistoricalSubjectKey> next)
    {
        if (visited.Contains(key))
        {
            return;
        }

        visited.Add(key);
        next.Add(key);
    }

    private static bool IsPublic(
        HistoricalSubject subject,
        IReadOnlySet<HistoricalSubjectKey> publicCurrentKeys)
    {
        return subject.PublicationPolicy switch
        {
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject => publicCurrentKeys.Contains(ToKey(subject)),
            HistoricalSubjectPublicationPolicy.HistoricalOnly => subject.Type is not HistoricalSubjectType.ParkItem
                    and not HistoricalSubjectType.ParkZone
                || publicCurrentKeys.Contains(ToKey(subject)),
            _ => false,
        };
    }

    private static HistoricalRelation[] SelectConnectedRelations(
        HistoricalSubjectKey root,
        IReadOnlyCollection<HistoricalRelation> relations)
    {
        HashSet<HistoricalSubjectKey> connected = new() { root };
        HashSet<Guid> selectedIds = new();
        bool changed;
        do
        {
            changed = false;
            foreach (HistoricalRelation relation in relations)
            {
                HistoricalSubjectKey source = ToKey(relation.Source);
                HistoricalSubjectKey target = ToKey(relation.Target);
                if (!connected.Contains(source) && !connected.Contains(target))
                {
                    continue;
                }

                changed |= selectedIds.Add(relation.Id);
                changed |= connected.Add(source);
                changed |= connected.Add(target);
            }
        }
        while (changed);

        return relations.Where(relation => selectedIds.Contains(relation.Id)).ToArray();
    }

    private static HistoricalSubjectKey ToKey(HistoricalSubject subject)
    {
        return new HistoricalSubjectKey(subject.Type, subject.Id, subject.ContextParkId);
    }
}
