using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class GetAdminHistoricalParkDiagnosticsQueryHandler :
    IQueryHandler<
        GetAdminHistoricalParkDiagnosticsQuery,
        ApplicationResult<AdminHistoricalParkDiagnosticsResult>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IHistoricalFactRepository factRepository;
    private readonly IHistoricalRelationRepository relationRepository;
    private readonly IHistoricalVisitDiagnosticsReader visitDiagnosticsReader;
    private readonly HistoricalParkDiagnosticsEvaluator diagnosticsEvaluator;

    public GetAdminHistoricalParkDiagnosticsQueryHandler(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IParkZoneRepository parkZoneRepository,
        IHistoricalFactRepository factRepository,
        IHistoricalRelationRepository relationRepository,
        IHistoricalVisitDiagnosticsReader visitDiagnosticsReader,
        HistoricalParkDiagnosticsEvaluator diagnosticsEvaluator)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.factRepository = factRepository;
        this.relationRepository = relationRepository;
        this.visitDiagnosticsReader = visitDiagnosticsReader;
        this.diagnosticsEvaluator = diagnosticsEvaluator;
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
        Task<Park?> parkTask = this.parkRepository.GetByIdAsync(parkId, true, cancellationToken);
        Task<IReadOnlyCollection<ParkItem>> itemsTask = this.parkItemRepository.GetByParkIdAsync(
            parkId,
            true,
            cancellationToken);
        Task<IReadOnlyCollection<ParkZone>> zonesTask = this.parkZoneRepository.GetByParkIdAsync(
            parkId,
            cancellationToken);
        await Task.WhenAll(parkTask, itemsTask, zonesTask);

        Park? park = await parkTask;
        if (park is null)
        {
            return ApplicationResult<AdminHistoricalParkDiagnosticsResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(Park), parkId));
        }

        IReadOnlyCollection<ParkItem> items = await itemsTask;
        IReadOnlyCollection<ParkZone> zones = await zonesTask;
        HistoricalSubject[] currentSubjects = BuildCurrentSubjects(park, items, zones);
        IReadOnlyCollection<HistoricalSubjectKey> currentSubjectKeys = currentSubjects
            .Select(static subject => new HistoricalSubjectKey(
                subject.Type,
                subject.Id,
                subject.ContextParkId))
            .ToArray();

        Task<IReadOnlyCollection<HistoricalFact>> factsTask =
            this.factRepository.GetLatestRevisionsForParkAsync(
                parkId,
                currentSubjects,
                cancellationToken);
        Task<IReadOnlyCollection<HistoricalRelation>> relationsTask =
            this.relationRepository.GetLatestRevisionsForParkAsync(
                parkId,
                currentSubjectKeys,
                cancellationToken);
        Task<HistoricalVisitDiagnosticCounts> visitsTask =
            this.visitDiagnosticsReader.GetCountsAsync(parkId, cancellationToken);
        await Task.WhenAll(factsTask, relationsTask, visitsTask);

        HistoricalParkDiagnostics diagnostics = this.diagnosticsEvaluator.Evaluate(
            await factsTask,
            await relationsTask,
            zones.Select(static zone => zone.Id).ToArray());
        return ApplicationResult<AdminHistoricalParkDiagnosticsResult>.Success(
            new AdminHistoricalParkDiagnosticsResult(
                park.Id,
                ResolveLabel(park.Name, "Park"),
                diagnostics,
                await visitsTask));
    }

    private static HistoricalSubject[] BuildCurrentSubjects(
        Park park,
        IReadOnlyCollection<ParkItem> items,
        IReadOnlyCollection<ParkZone> zones)
    {
        return new[]
            {
                new HistoricalSubject(
                    HistoricalSubjectType.Park,
                    park.Id,
                    ResolveLabel(park.Name, "Park"),
                    HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                    park.Id),
            }
            .Concat(items.Select(item => new HistoricalSubject(
                HistoricalSubjectType.ParkItem,
                item.Id,
                ResolveLabel(item.Name, "Park item"),
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                park.Id)))
            .Concat(zones.Select(zone => new HistoricalSubject(
                HistoricalSubjectType.ParkZone,
                zone.Id,
                ResolveLabel(zone.Name, "Park zone"),
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                park.Id)))
            .DistinctBy(static subject => (subject.Type, subject.Id))
            .ToArray();
    }

    private static string ResolveLabel(string? label, string fallback)
    {
        return string.IsNullOrWhiteSpace(label) ? fallback : label.Trim();
    }
}
