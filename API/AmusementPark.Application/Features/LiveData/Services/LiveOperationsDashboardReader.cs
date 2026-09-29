using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class LiveOperationsDashboardReader
{
    private const int MaximumMappings = 500;
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
        Task<PagedResult<ExternalLiveTargetMapping>> mappingsTask =
            this.mappingRepository.SearchLatestAsync(
                new LiveTargetMappingSearchCriteria(
                    1,
                    MaximumMappings,
                    target.SourceId.Value,
                    null,
                    null,
                    null),
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
        await Task.WhenAll(mappingsTask, pollingTask, incidentsTask, gateTask);

        PagedResult<ExternalLiveTargetMapping> mappingPage = await mappingsTask;
        LiveOperationalGateSnapshot gate = await gateTask;
        ExternalLiveTargetMapping[] relevantMappings = mappingPage.Items
            .Where(mapping => IsInConfiguredEntity(mapping, target.ExternalEntityId))
            .ToArray();
        LiveOperationalScopeResult[] scopes = BuildScopes(
            source,
            target,
            relevantMappings,
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
                relevantMappings.Length,
                relevantMappings.Count(static mapping => mapping.IsEligibleForLiveUse),
                relevantMappings.Count(static mapping => mapping.Status == LiveMappingStatus.Candidate),
                relevantMappings.Count(static mapping => mapping.Status == LiveMappingStatus.Suspended),
                await incidentsTask,
                mappingPage.TotalItems > MaximumMappings),
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
        definitions.AddRange(mappings
            .Where(static mapping => mapping.IsEligibleForLiveUse && mapping.Target is not null)
            .Select(mapping =>
            {
                LiveTargetReference reference = mapping.Target!;
                LiveOperationalScopeType scopeType = reference.Type == LiveTargetType.Park
                    ? LiveOperationalScopeType.Park
                    : LiveOperationalScopeType.Target;
                return (
                    new LiveOperationalControlScope(
                        scopeType,
                        target.SourceId,
                        target.ExternalEntityId,
                        reference.ParkId,
                        scopeType == LiveOperationalScopeType.Target ? reference.Type : null,
                        scopeType == LiveOperationalScopeType.Target ? reference.Id : null),
                    reference.DisplayName,
                    reference.Type == LiveTargetType.Park ? null : reference.ParkDisplayName);
            })
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

    private static bool IsInConfiguredEntity(
        ExternalLiveTargetMapping mapping,
        string externalEntityId)
    {
        return string.Equals(
                mapping.ExternalTarget.Id,
                externalEntityId,
                StringComparison.Ordinal)
            || string.Equals(
                mapping.ExternalTarget.ParentId,
                externalEntityId,
                StringComparison.Ordinal);
    }
}
