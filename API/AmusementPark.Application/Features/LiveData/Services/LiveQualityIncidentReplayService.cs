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
    private readonly ILiveLatestObservationRepository latestRepository;
    private readonly ILiveOperationalGate operationalGate;
    private readonly TimeProvider timeProvider;

    public LiveQualityIncidentReplayService(
        ILiveQualityIncidentRepository incidentRepository,
        ILiveTargetMappingRepository mappingRepository,
        ILiveLatestObservationRepository latestRepository,
        ILiveOperationalGate operationalGate)
        : this(
            incidentRepository,
            mappingRepository,
            latestRepository,
            operationalGate,
            TimeProvider.System)
    {
    }

    internal LiveQualityIncidentReplayService(
        ILiveQualityIncidentRepository incidentRepository,
        ILiveTargetMappingRepository mappingRepository,
        ILiveLatestObservationRepository latestRepository,
        ILiveOperationalGate operationalGate,
        TimeProvider timeProvider)
    {
        this.incidentRepository = incidentRepository
            ?? throw new ArgumentNullException(nameof(incidentRepository));
        this.mappingRepository = mappingRepository
            ?? throw new ArgumentNullException(nameof(mappingRepository));
        this.latestRepository = latestRepository
            ?? throw new ArgumentNullException(nameof(latestRepository));
        this.operationalGate = operationalGate
            ?? throw new ArgumentNullException(nameof(operationalGate));
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
        IReadOnlyCollection<LiveQualityIncident> candidates =
            await this.incidentRepository.GetReplayCandidatesAsync(
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
        List<Guid> resolvedIds = new List<Guid>();
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
                }

                resolvedIds.Add(incident.Id);
            }
        }

        LiveLatestObservationWriteResult writeResult = await this.latestRepository.WriteLatestAsync(
            latestByInternalTarget.Values.ToArray(),
            cancellationToken);
        int resolvedCount = await this.incidentRepository.MarkResolvedAsync(
            resolvedIds,
            replayedAtUtc,
            normalizedResolvedByUserId,
            cancellationToken);
        return new LiveQualityReplayResult(
            candidates.Count,
            resolvedCount,
            candidates.Count - resolvedCount,
            writeResult.InsertedCount + writeResult.UpdatedCount,
            writeResult.IgnoredCount);
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
