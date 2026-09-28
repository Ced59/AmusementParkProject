using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class GetAdminHistoricalParkWorkbenchQueryHandler :
    IQueryHandler<
        GetAdminHistoricalParkWorkbenchQuery,
        ApplicationResult<AdminHistoricalParkWorkbenchResult>>
{
    private const int RecentSourceLimit = 100;

    private readonly HistoricalParkEditorialScopeLoader scopeLoader;
    private readonly IHistoricalFactRepository factRepository;
    private readonly IHistoricalRelationRepository relationRepository;
    private readonly IHistoricalSourceRepository sourceRepository;
    private readonly IHistoricalVisitDiagnosticsReader visitDiagnosticsReader;
    private readonly HistoricalParkDiagnosticsEvaluator diagnosticsEvaluator;

    public GetAdminHistoricalParkWorkbenchQueryHandler(
        HistoricalParkEditorialScopeLoader scopeLoader,
        IHistoricalFactRepository factRepository,
        IHistoricalRelationRepository relationRepository,
        IHistoricalSourceRepository sourceRepository,
        IHistoricalVisitDiagnosticsReader visitDiagnosticsReader,
        HistoricalParkDiagnosticsEvaluator diagnosticsEvaluator)
    {
        this.scopeLoader = scopeLoader;
        this.factRepository = factRepository;
        this.relationRepository = relationRepository;
        this.sourceRepository = sourceRepository;
        this.visitDiagnosticsReader = visitDiagnosticsReader;
        this.diagnosticsEvaluator = diagnosticsEvaluator;
    }

    public async Task<ApplicationResult<AdminHistoricalParkWorkbenchResult>> HandleAsync(
        GetAdminHistoricalParkWorkbenchQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<AdminHistoricalParkWorkbenchResult>.Failure(
                ApplicationErrors.Required("parkId"));
        }

        HistoricalParkEditorialScope? scope = await this.scopeLoader.LoadAsync(
            query.ParkId,
            cancellationToken);
        if (scope is null)
        {
            return ApplicationResult<AdminHistoricalParkWorkbenchResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(Park), query.ParkId));
        }

        HistoricalSubjectKey[] subjectKeys = scope.CurrentSubjects
            .Select(static subject => new HistoricalSubjectKey(
                subject.Type,
                subject.Id,
                subject.ContextParkId))
            .ToArray();
        Task<IReadOnlyCollection<HistoricalFact>> factsTask =
            this.factRepository.GetLatestRevisionsForParkAsync(
                scope.ParkId,
                scope.CurrentSubjects,
                cancellationToken);
        Task<IReadOnlyCollection<HistoricalRelation>> relationsTask =
            this.relationRepository.GetLatestRevisionsForParkAsync(
                scope.ParkId,
                subjectKeys,
                cancellationToken);
        Task<IReadOnlyCollection<HistoricalSourceReference>> recentSourcesTask =
            this.sourceRepository.GetRecentLatestRevisionsAsync(
                RecentSourceLimit,
                cancellationToken);
        Task<HistoricalVisitDiagnosticCounts> visitsTask =
            this.visitDiagnosticsReader.GetCountsAsync(scope.ParkId, cancellationToken);
        await Task.WhenAll(factsTask, relationsTask, recentSourcesTask, visitsTask);

        IReadOnlyCollection<HistoricalFact> facts = await factsTask;
        IReadOnlyCollection<HistoricalRelation> relations = await relationsTask;
        IReadOnlyCollection<HistoricalSourceReference> recentSources = await recentSourcesTask;
        Guid[] referencedSourceIds = facts
            .SelectMany(static fact => fact.SourceReferences)
            .Select(static reference => reference.SourceId)
            .Concat(relations
                .SelectMany(static relation => relation.SourceReferences)
                .Select(static reference => reference.SourceId))
            .Distinct()
            .ToArray();
        IReadOnlyCollection<HistoricalSourceReference> referencedSources =
            await this.sourceRepository.GetLatestRevisionsAsync(
                referencedSourceIds,
                cancellationToken);
        HistoricalSourceReference[] sources = recentSources
            .Concat(referencedSources)
            .DistinctBy(static source => source.Id)
            .OrderByDescending(static source => source.RecordedAtUtc)
            .ToArray();
        HistoricalSubject[] subjects = scope.CurrentSubjects
            .Concat(facts.Select(static fact => fact.Subject))
            .Concat(relations.SelectMany(static relation => new[]
            {
                relation.Source,
                relation.Target,
            }))
            .DistinctBy(static subject => (subject.Type, subject.Id))
            .OrderBy(static subject => subject.Type)
            .ThenBy(static subject => subject.HistoricalLabel, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        HistoricalParkDiagnostics diagnostics = this.diagnosticsEvaluator.Evaluate(
            facts,
            relations,
            scope.CurrentZoneIds);

        return ApplicationResult<AdminHistoricalParkWorkbenchResult>.Success(
            new AdminHistoricalParkWorkbenchResult(
                scope.ParkId,
                scope.ParkName,
                subjects,
                facts.OrderByDescending(static fact => fact.RecordedAtUtc).ToArray(),
                relations.OrderByDescending(static relation => relation.RecordedAtUtc).ToArray(),
                sources,
                diagnostics,
                await visitsTask));
    }
}
