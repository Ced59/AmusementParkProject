using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class LiveQualityIncidentReplayService
{
    private const int MaximumBatchSize = 100;
    private readonly ILiveQualityIncidentRepository incidentRepository;
    private readonly ILiveTargetMappingRepository mappingRepository;
    private readonly ILiveDataSourceCatalog sourceCatalog;
    private readonly ILiveLatestObservationRepository latestRepository;
    private readonly ILiveOperationalGate operationalGate;
    private readonly LiveOperationalObservationWriter operationalWriter;
    private readonly TimeProvider timeProvider;

    internal LiveQualityIncidentReplayService(
        ILiveQualityIncidentRepository incidentRepository,
        ILiveTargetMappingRepository mappingRepository,
        ILiveLatestObservationRepository latestRepository,
        ILiveOperationalGate operationalGate,
        ILiveDataSourceCatalog sourceCatalog)
        : this(
            incidentRepository,
            mappingRepository,
            latestRepository,
            operationalGate,
            sourceCatalog,
            new LiveOperationalWriteCoordinator(),
            TimeProvider.System)
    {
    }

    public LiveQualityIncidentReplayService(
        ILiveQualityIncidentRepository incidentRepository,
        ILiveTargetMappingRepository mappingRepository,
        ILiveLatestObservationRepository latestRepository,
        ILiveOperationalGate operationalGate,
        ILiveDataSourceCatalog sourceCatalog,
        LiveOperationalWriteCoordinator coordinator)
        : this(
            incidentRepository,
            mappingRepository,
            latestRepository,
            operationalGate,
            sourceCatalog,
            coordinator,
            TimeProvider.System)
    {
    }

    internal LiveQualityIncidentReplayService(
        ILiveQualityIncidentRepository incidentRepository,
        ILiveTargetMappingRepository mappingRepository,
        ILiveLatestObservationRepository latestRepository,
        ILiveOperationalGate operationalGate,
        ILiveDataSourceCatalog sourceCatalog,
        TimeProvider timeProvider)
        : this(
            incidentRepository,
            mappingRepository,
            latestRepository,
            operationalGate,
            sourceCatalog,
            new LiveOperationalWriteCoordinator(),
            timeProvider)
    {
    }

    internal LiveQualityIncidentReplayService(
        ILiveQualityIncidentRepository incidentRepository,
        ILiveTargetMappingRepository mappingRepository,
        ILiveLatestObservationRepository latestRepository,
        ILiveOperationalGate operationalGate,
        ILiveDataSourceCatalog sourceCatalog,
        LiveOperationalWriteCoordinator coordinator,
        TimeProvider timeProvider)
    {
        this.incidentRepository = incidentRepository
            ?? throw new ArgumentNullException(nameof(incidentRepository));
        this.mappingRepository = mappingRepository
            ?? throw new ArgumentNullException(nameof(mappingRepository));
        this.sourceCatalog = sourceCatalog
            ?? throw new ArgumentNullException(nameof(sourceCatalog));
        this.latestRepository = latestRepository
            ?? throw new ArgumentNullException(nameof(latestRepository));
        this.operationalGate = operationalGate
            ?? throw new ArgumentNullException(nameof(operationalGate));
        this.operationalWriter = new LiveOperationalObservationWriter(
            this.latestRepository,
            this.operationalGate,
            coordinator);
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<LiveQualityReplayResult> ReplayAsync(
        int maximumCount,
        string resolvedByUserId,
        CancellationToken cancellationToken)
    {
        if (maximumCount is < 1 or > MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount));
        }

        string normalizedResolvedByUserId = IdentifierRules.NormalizeRequired(
            resolvedByUserId,
            nameof(resolvedByUserId));

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        LivePollingTarget? configuredTarget = this.sourceCatalog.ConfiguredPollingTarget;
        if (configuredTarget is null)
        {
            return new LiveQualityReplayResult(0, 0, 0, 0, 0);
        }

        IReadOnlyCollection<ExternalLiveTargetMapping> configuredMappings =
            await this.mappingRepository.GetLatestByExternalEntityAsync(
                configuredTarget.SourceId,
                configuredTarget.ExternalEntityId,
                cancellationToken);
        string[] configuredExternalTargetIds = configuredMappings
            .Select(static mapping => mapping.ExternalTarget.Id)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyCollection<LiveQualityIncident> candidates =
            await this.incidentRepository.GetReplayCandidatesAsync(
                configuredTarget.SourceId,
                configuredExternalTargetIds,
                maximumCount,
                nowUtc,
                cancellationToken);
        DateTime replayedAtUtc = candidates.Aggregate(
            nowUtc,
            static (current, incident) =>
            {
                DateTime incidentFloor = incident.DetectedAtUtc > incident.ReceivedAtUtc
                    ? incident.DetectedAtUtc
                    : incident.ReceivedAtUtc;
                return incidentFloor > current ? incidentFloor : current;
            });
        if (candidates.Count > 0)
        {
            await this.incidentRepository.MarkReplayAttemptedAsync(
                candidates.Select(static incident => incident.Id).ToArray(),
                replayedAtUtc,
                cancellationToken);
        }

        Dictionary<
            (LiveDataSourceId SourceId, LiveTargetType TargetType, string TargetId),
            LiveLatestObservation> latestByInternalTarget = new();
        Dictionary<
            (LiveDataSourceId SourceId, string ExternalEntityId),
            LiveOperationalGateSnapshot> gates = new();
        Dictionary<
            (LiveDataSourceId SourceId, LiveTargetType TargetType, string TargetId),
            string> externalEntityByInternalTarget = new();
        Dictionary<
            Guid,
            (LiveDataSourceId SourceId, LiveTargetType TargetType, string TargetId)>
            resolvableIncidentTargets = new();
        foreach (IGrouping<LiveDataSourceId, LiveQualityIncident> sourceGroup in candidates
                     .GroupBy(static incident => incident.SourceId))
        {
            string[] externalIds = sourceGroup
                .Select(static incident => incident.Observation!.ExternalTargetId)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            IReadOnlyCollection<ExternalLiveTargetMapping> mappings =
                await this.mappingRepository.GetLatestByExternalTargetIdsAsync(
                    sourceGroup.Key,
                    externalIds,
                    cancellationToken);
            Dictionary<string, ExternalLiveTargetMapping> mappingsByExternalId = mappings
                .ToDictionary(static mapping => mapping.ExternalTarget.Id, StringComparer.Ordinal);

            foreach (LiveQualityIncident incident in sourceGroup)
            {
                ExternalLiveObservation observation = incident.Observation!;
                if (!mappingsByExternalId.TryGetValue(
                        observation.ExternalTargetId,
                        out ExternalLiveTargetMapping? mapping)
                    || !mapping.IsEligibleForLiveUse
                    || mapping.Target is null
                    || mapping.ExternalTarget.Type != observation.TargetType
                    || mapping.Target.Type != observation.TargetType)
                {
                    continue;
                }

                string externalEntityId = mapping.ExternalTarget.Type == LiveTargetType.Park
                    ? mapping.ExternalTarget.Id
                    : mapping.ExternalTarget.ParentId!;
                (LiveDataSourceId, string) gateKey = (incident.SourceId, externalEntityId);
                if (!gates.TryGetValue(gateKey, out LiveOperationalGateSnapshot? gate))
                {
                    gate = await this.operationalGate.LoadAsync(
                        incident.SourceId,
                        externalEntityId,
                        cancellationToken);
                    gates[gateKey] = gate;
                }

                if (!gate.AllowsCollection(
                        mapping.Target.ParkId,
                        mapping.Target.Type,
                        mapping.Target.Id))
                {
                    continue;
                }

                LiveFreshnessAssessment freshness = incident.FreshnessPolicy.Assess(
                    observation.SourceUpdatedAtUtc,
                    replayedAtUtc);
                if (freshness.State == LiveFreshnessState.Unavailable
                    || observation.HasStatusQueueConflict)
                {
                    continue;
                }

                LiveLatestObservation candidate = new LiveLatestObservation(
                    mapping.Target,
                    observation.Status,
                    observation.Queues,
                    new LiveObservationProvenance(
                        incident.SourceId,
                        observation.ExternalTargetId,
                        observation.SourceUpdatedAtUtc,
                        incident.ReceivedAtUtc,
                        replayedAtUtc,
                        incident.CorrelationId,
                        incident.AdapterVersion,
                        mapping.Version,
                        incident.Confidence,
                        incident.UsagePolicyVersion,
                        incident.TransformationVersion),
                    incident.FreshnessPolicy,
                    incident.PayloadSha256);
                (LiveDataSourceId, LiveTargetType, string) key = (
                    incident.SourceId,
                    mapping.Target.Type,
                    mapping.Target.Id);
                if (!latestByInternalTarget.TryGetValue(key, out LiveLatestObservation? current)
                    || IsPreferred(candidate, current))
                {
                    latestByInternalTarget[key] = candidate;
                    externalEntityByInternalTarget[key] = externalEntityId;
                }

                resolvableIncidentTargets[incident.Id] = key;
            }
        }

        int persistedCount = 0;
        int ignoredCount = 0;
        HashSet<
            (LiveDataSourceId SourceId, LiveTargetType TargetType, string TargetId)>
            acceptedTargets = new();
        if (latestByInternalTarget.Count == 0)
        {
            LiveLatestObservationWriteResult emptyWrite =
                await this.latestRepository.WriteLatestAsync(
                    Array.Empty<LiveLatestObservation>(),
                    cancellationToken);
            ignoredCount = emptyWrite.IgnoredCount;
        }
        else
        {
            IEnumerable<IGrouping<
                (LiveDataSourceId SourceId, string ExternalEntityId),
                KeyValuePair<
                    (LiveDataSourceId SourceId, LiveTargetType TargetType, string TargetId),
                    LiveLatestObservation>>> writeGroups = latestByInternalTarget.GroupBy(pair =>
                        (pair.Key.SourceId, externalEntityByInternalTarget[pair.Key]));
            foreach (IGrouping<
                         (LiveDataSourceId SourceId, string ExternalEntityId),
                         KeyValuePair<
                             (LiveDataSourceId SourceId, LiveTargetType TargetType, string TargetId),
                             LiveLatestObservation>> writeGroup in writeGroups)
            {
                LiveOperationalObservationWriteResult operationalWrite =
                    await this.operationalWriter.WriteAsync(
                        writeGroup.Key.SourceId,
                        writeGroup.Key.ExternalEntityId,
                        writeGroup.Select(static pair => pair.Value).ToArray(),
                        cancellationToken);
                persistedCount += operationalWrite.WriteResult.InsertedCount
                    + operationalWrite.WriteResult.UpdatedCount;
                ignoredCount += operationalWrite.WriteResult.IgnoredCount;
                foreach (LiveLatestObservation accepted in operationalWrite.AcceptedObservations)
                {
                    acceptedTargets.Add((
                        writeGroup.Key.SourceId,
                        accepted.Target.Type,
                        accepted.Target.Id));
                }
            }
        }

        Guid[] resolvedIds = resolvableIncidentTargets
            .Where(pair => acceptedTargets.Contains(pair.Value))
            .Select(static pair => pair.Key)
            .ToArray();
        int resolvedCount = await this.incidentRepository.MarkResolvedAsync(
            resolvedIds,
            replayedAtUtc,
            normalizedResolvedByUserId,
            cancellationToken);
        return new LiveQualityReplayResult(
            candidates.Count,
            resolvedCount,
            candidates.Count - resolvedCount,
            persistedCount,
            ignoredCount);
    }

    private static bool IsPreferred(
        LiveLatestObservation candidate,
        LiveLatestObservation current)
    {
        int comparison = candidate.CompareRecencyTo(current);
        return comparison > 0
            || (comparison == 0
                && string.CompareOrdinal(
                    candidate.Provenance.ExternalTargetId,
                    current.Provenance.ExternalTargetId) < 0);
    }
}
