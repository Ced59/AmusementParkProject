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

public sealed class GetAdminHistoricalParkDiagnosticsQueryHandler :
    IQueryHandler<
        GetAdminHistoricalParkDiagnosticsQuery,
        ApplicationResult<AdminHistoricalParkDiagnosticsResult>>
{
    private readonly HistoricalParkEditorialScopeLoader scopeLoader;
    private readonly IHistoricalFactRepository factRepository;
    private readonly IHistoricalRelationRepository relationRepository;
    private readonly IHistoricalVisitDiagnosticsReader visitDiagnosticsReader;
    private readonly HistoricalParkDiagnosticsEvaluator diagnosticsEvaluator;
    private readonly IHistoricalParkRolloutGateAssessmentService rolloutGateAssessmentService;

    public GetAdminHistoricalParkDiagnosticsQueryHandler(
        HistoricalParkEditorialScopeLoader scopeLoader,
        IHistoricalFactRepository factRepository,
        IHistoricalRelationRepository relationRepository,
        IHistoricalVisitDiagnosticsReader visitDiagnosticsReader,
        HistoricalParkDiagnosticsEvaluator diagnosticsEvaluator,
        IHistoricalParkRolloutGateAssessmentService rolloutGateAssessmentService)
    {
        this.scopeLoader = scopeLoader;
        this.factRepository = factRepository;
        this.relationRepository = relationRepository;
        this.visitDiagnosticsReader = visitDiagnosticsReader;
        this.diagnosticsEvaluator = diagnosticsEvaluator;
        this.rolloutGateAssessmentService = rolloutGateAssessmentService;
    }

    public async Task<ApplicationResult<AdminHistoricalParkDiagnosticsResult>> HandleAsync(
        GetAdminHistoricalParkDiagnosticsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<AdminHistoricalParkDiagnosticsResult>.Failure(
                ApplicationErrors.Required("parkId"));
        }

        string parkId = query.ParkId.Trim();
        HistoricalParkEditorialScope? scope = await this.scopeLoader.LoadAsync(
            parkId,
            cancellationToken);
        if (scope is null)
        {
            return ApplicationResult<AdminHistoricalParkDiagnosticsResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(Park), parkId));
        }

        IReadOnlyCollection<HistoricalSubjectKey> currentSubjectKeys = scope.CurrentSubjects
            .Select(static subject => new HistoricalSubjectKey(
                subject.Type,
                subject.Id,
                subject.ContextParkId))
            .ToArray();

        Task<IReadOnlyCollection<HistoricalFact>> factsTask =
            this.factRepository.GetLatestRevisionsForParkAsync(
                parkId,
                scope.CurrentSubjects,
                cancellationToken);
        Task<IReadOnlyCollection<HistoricalRelation>> relationsTask =
            this.relationRepository.GetLatestRevisionsForParkAsync(
                parkId,
                currentSubjectKeys,
                cancellationToken);
        Task<HistoricalVisitDiagnosticCounts> visitsTask =
            this.visitDiagnosticsReader.GetCountsAsync(parkId, cancellationToken);
        await Task.WhenAll(factsTask, relationsTask, visitsTask);

        IReadOnlyCollection<HistoricalFact> facts = await factsTask;
        HistoricalParkDiagnostics diagnostics = this.diagnosticsEvaluator.Evaluate(
            facts,
            await relationsTask,
            scope.CurrentZoneIds);
        HistoricalParkRolloutGate rolloutGate =
            await this.rolloutGateAssessmentService.AssessPublicParkAsync(
                scope,
                facts,
                cancellationToken);
        return ApplicationResult<AdminHistoricalParkDiagnosticsResult>.Success(
            new AdminHistoricalParkDiagnosticsResult(
                scope.ParkId,
                scope.ParkName,
                diagnostics,
                await visitsTask,
                rolloutGate));
    }

}
