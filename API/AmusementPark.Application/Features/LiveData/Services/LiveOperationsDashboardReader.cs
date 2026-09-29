using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class LiveOperationsDashboardReader
{
    private readonly ILiveDataSourceCatalog sourceCatalog;
    private readonly IReadOnlyCollection<ILiveDataProviderAdapter> adapters;
    private readonly ILiveTargetMappingRepository mappingRepository;
    private readonly ILivePollingStateRepository pollingStateRepository;
    private readonly ILiveQualityIncidentRepository incidentRepository;
    private readonly ILiveOperationalGate operationalGate;
    private readonly LiveOperationalScopeResultFactory scopeResultFactory;
    private readonly TimeProvider timeProvider;

    public LiveOperationsDashboardReader(
        ILiveDataSourceCatalog sourceCatalog,
        IEnumerable<ILiveDataProviderAdapter> adapters,
        ILiveTargetMappingRepository mappingRepository,
        ILivePollingStateRepository pollingStateRepository,
        ILiveQualityIncidentRepository incidentRepository,
        ILiveOperationalGate operationalGate,
        LiveOperationalScopeResultFactory scopeResultFactory,
        TimeProvider? timeProvider = null)
    {
        this.sourceCatalog = sourceCatalog;
        this.adapters = adapters.ToArray();
        this.mappingRepository = mappingRepository;
        this.pollingStateRepository = pollingStateRepository;
        this.incidentRepository = incidentRepository;
        this.operationalGate = operationalGate;
        this.scopeResultFactory = scopeResultFactory;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<LiveOperationsDashboardResult>> ReadAsync(
        CancellationToken cancellationToken)
    {
        LivePollingTarget? target = this.sourceCatalog.ConfiguredPollingTarget;
        LiveDataSourcePresentation? source = target is null
            ? null
            : this.sourceCatalog.Find(target.SourceId);
        ILiveDataProviderAdapter? adapter = target is null
            ? null
            : this.adapters.SingleOrDefault(candidate => candidate.SourceId == target.SourceId);
        if (target is null || source is null || adapter is null)
        {
            return ApplicationResult<LiveOperationsDashboardResult>.Failure(
                LiveDataApplicationErrors.OperationsUnavailable());
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        Task<IReadOnlyCollection<ExternalLiveTargetMapping>> mappingsTask =
            this.mappingRepository.GetLatestByExternalEntityAsync(
                target.SourceId,
                target.ExternalEntityId,
                cancellationToken);
        Task<LivePollingStateSnapshot?> pollingTask = this.pollingStateRepository.GetAsync(
            target.SourceId,
            target.ExternalEntityId,
            cancellationToken);
        Task<long> incidentsTask = this.incidentRepository.CountPendingAsync(
            target.SourceId,
            nowUtc,
            cancellationToken);
        Task<LiveOperationalGateSnapshot> gateTask = this.operationalGate.LoadAsync(
            target.SourceId,
            target.ExternalEntityId,
            cancellationToken);
        await Task.WhenAll(
            mappingsTask,
            pollingTask,
            incidentsTask,
            gateTask);

        IReadOnlyCollection<ExternalLiveTargetMapping> mappings = await mappingsTask;
        string[] configuredExternalTargetIds = mappings
            .Where(static mapping => mapping.IsEligibleForLiveUse)
            .Select(static mapping => mapping.ExternalTarget.Id)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        long replayableIncidentCount =
            await this.incidentRepository.CountReplayablePendingAsync(
                target.SourceId,
                configuredExternalTargetIds,
                nowUtc,
                cancellationToken);
        LiveOperationalGateSnapshot gate = await gateTask;
        LiveOperationalScopeResult[] scopes = BuildScopes(
            source,
            target,
            mappings,
            gate,
            this.scopeResultFactory);
        LivePollingStateSnapshot? polling = await pollingTask;
        LiveOperationsDashboardResult result = new LiveOperationsDashboardResult(
            target.SourceId.Value,
            source.Source.DisplayName,
            gate.ConfiguredCollectionEnabled,
            gate.ConfiguredPublicReadEnabled,
            adapter.AdapterVersion,
            adapter.TransformationVersion,
            adapter.UsagePolicyVersion,
            source.Source.UsagePolicy.TermsUrl,
            source.Source.UsagePolicy.ReviewedAtUtc,
            source.AttributionText,
            source.AttributionUrl,
            new LiveOperationsPollingResult(
                target.ExternalEntityId,
                polling?.NextAttemptAtUtc,
                polling?.LastPolledAtUtc,
                polling?.LastSuccessfulPollAtUtc,
                polling?.ConsecutiveFailures ?? 0,
                polling?.CircuitOpenUntilUtc,
                polling?.LastDisposition,
                polling?.LeaseActive ?? false,
                polling?.LeaseExpiresAtUtc),
            new LiveOperationsSummaryResult(
                mappings.Count,
                mappings.Count(static mapping => mapping.IsEligibleForLiveUse),
                mappings.Count(static mapping => mapping.Status == LiveMappingStatus.Candidate),
                mappings.Count(static mapping => mapping.Status == LiveMappingStatus.Suspended),
                await incidentsTask,
                replayableIncidentCount),
            scopes,
            nowUtc);
        return ApplicationResult<LiveOperationsDashboardResult>.Success(result);
    }

    private static LiveOperationalScopeResult[] BuildScopes(
        LiveDataSourcePresentation source,
        LivePollingTarget target,
        IReadOnlyCollection<ExternalLiveTargetMapping> mappings,
        LiveOperationalGateSnapshot gate,
        LiveOperationalScopeResultFactory factory)
    {
        List<(LiveOperationalControlScope Scope, string Name, string? ParentName)> definitions =
            new List<(LiveOperationalControlScope, string, string?)>
            {
                (
                    new LiveOperationalControlScope(
                        LiveOperationalScopeType.Source,
                        target.SourceId,
                        null,
                        null,
                        null,
                        null),
                    source.Source.DisplayName,
                    null),
            };
        LiveTargetReference[] eligibleReferences = mappings
            .Where(static mapping => mapping.IsEligibleForLiveUse && mapping.Target is not null)
            .Select(static mapping => mapping.Target!)
            .ToArray();
        definitions.AddRange(eligibleReferences
            .Select(reference => (
                new LiveOperationalControlScope(
                    LiveOperationalScopeType.Park,
                    target.SourceId,
                    target.ExternalEntityId,
                    reference.ParkId,
                    null,
                    null),
                reference.ParkDisplayName,
                (string?)null))
            .DistinctBy(static definition => definition.Item1));
        definitions.AddRange(eligibleReferences
            .Where(static reference => reference.Type != LiveTargetType.Park)
            .Select(reference => (
                    new LiveOperationalControlScope(
                        LiveOperationalScopeType.Target,
                        target.SourceId,
                        target.ExternalEntityId,
                        reference.ParkId,
                        reference.Type,
                        reference.Id),
                    reference.DisplayName,
                    (string?)reference.ParkDisplayName))
            .DistinctBy(static definition => definition.Item1));
        return definitions
            .OrderBy(static definition => definition.Scope.Type)
            .ThenBy(static definition => definition.Name, StringComparer.OrdinalIgnoreCase)
            .Select(definition => factory.Create(
                definition.Scope,
                definition.Name,
                definition.ParentName,
                gate.Controls.FirstOrDefault(control => control.Scope.Equals(definition.Scope)),
                gate))
            .ToArray();
    }

}
