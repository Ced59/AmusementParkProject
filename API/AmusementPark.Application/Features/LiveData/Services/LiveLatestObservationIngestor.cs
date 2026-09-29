using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class LiveLatestObservationIngestor : ILiveLatestObservationIngestor
{
    private static readonly TimeSpan IncidentRetention = TimeSpan.FromDays(7);
    private readonly ILiveTargetMappingRepository mappingRepository;
    private readonly ILiveLatestObservationRepository latestRepository;
    private readonly ILiveQualityIncidentRepository incidentRepository;
    private readonly TimeProvider timeProvider;

    public LiveLatestObservationIngestor(
        ILiveTargetMappingRepository mappingRepository,
        ILiveLatestObservationRepository latestRepository,
        ILiveQualityIncidentRepository incidentRepository)
        : this(mappingRepository, latestRepository, incidentRepository, TimeProvider.System)
    {
    }

    internal LiveLatestObservationIngestor(
        ILiveTargetMappingRepository mappingRepository,
        ILiveLatestObservationRepository latestRepository,
        ILiveQualityIncidentRepository incidentRepository,
        TimeProvider timeProvider)
    {
        this.mappingRepository = mappingRepository
            ?? throw new ArgumentNullException(nameof(mappingRepository));
        this.latestRepository = latestRepository
            ?? throw new ArgumentNullException(nameof(latestRepository));
        this.incidentRepository = incidentRepository
            ?? throw new ArgumentNullException(nameof(incidentRepository));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<LiveLatestObservationIngestionResult> IngestAsync(
        LiveLatestObservationIngestionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ReceivedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The live ingestion reception timestamp must be UTC.", nameof(request));
        }

        string[] externalTargetIds = request.Observations
            .Select(static observation => observation.ExternalTargetId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyCollection<ExternalLiveTargetMapping> mappings =
            await this.mappingRepository.GetLatestByExternalTargetIdsAsync(
                request.SourceId,
                externalTargetIds,
                cancellationToken);
        Dictionary<string, ExternalLiveTargetMapping> mappingsByExternalId = mappings
            .ToDictionary(
                static mapping => mapping.ExternalTarget.Id,
                StringComparer.Ordinal);
        Dictionary<string, LiveLatestObservation> latestByInternalTarget =
            new Dictionary<string, LiveLatestObservation>(StringComparer.Ordinal);
        List<LiveQualityIncident> incidents = new List<LiveQualityIncident>();
        int unmappedCount = 0;
        int ineligibleCount = 0;
        int invalidFreshnessCount = 0;
        DateTime normalizedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        if (normalizedAtUtc < request.ReceivedAtUtc)
        {
            normalizedAtUtc = request.ReceivedAtUtc;
        }

        string correlationId = Guid.NewGuid().ToString("N");
        foreach (ExternalLiveObservation observation in request.Observations)
        {
            if (!mappingsByExternalId.TryGetValue(observation.ExternalTargetId, out ExternalLiveTargetMapping? mapping))
            {
                unmappedCount++;
                incidents.Add(CreateIncident(
                    request,
                    observation,
                    LiveQualityIncidentReason.UnmappedTarget,
                    null,
                    null,
                    null,
                    normalizedAtUtc,
                    correlationId));
                continue;
            }

            if (!mapping.IsEligibleForLiveUse
                || mapping.Target is null
                || mapping.ExternalTarget.Type != observation.TargetType
                || mapping.Target.Type != observation.TargetType)
            {
                ineligibleCount++;
                incidents.Add(CreateIncident(
                    request,
                    observation,
                    LiveQualityIncidentReason.IneligibleMapping,
                    null,
                    null,
                    null,
                    normalizedAtUtc,
                    correlationId));
                continue;
            }

            LiveFreshnessAssessment freshness = request.FreshnessPolicy.Assess(
                observation.SourceUpdatedAtUtc,
                normalizedAtUtc);
            if (freshness.State == LiveFreshnessState.Unavailable)
            {
                invalidFreshnessCount++;
                incidents.Add(CreateIncident(
                    request,
                    observation,
                    LiveQualityIncidentReason.InvalidFreshness,
                    null,
                    null,
                    null,
                    normalizedAtUtc,
                    correlationId));
                continue;
            }

            if (observation.HasStatusQueueConflict)
            {
                incidents.Add(CreateIncident(
                    request,
                    observation,
                    LiveQualityIncidentReason.StatusQueueConflict,
                    LiveProviderDiagnosticCodes.StatusQueueConflict,
                    observation.ExternalTargetId,
                    "queues",
                    normalizedAtUtc,
                    correlationId));
                continue;
            }

            LiveObservationProvenance provenance = new LiveObservationProvenance(
                request.SourceId,
                observation.ExternalTargetId,
                observation.SourceUpdatedAtUtc,
                request.ReceivedAtUtc,
                normalizedAtUtc,
                correlationId,
                request.AdapterVersion,
                mapping.Version,
                request.Confidence,
                request.UsagePolicyVersion,
                request.TransformationVersion);
            LiveLatestObservation latest = new LiveLatestObservation(
                mapping.Target,
                observation.Status,
                observation.Queues,
                provenance,
                request.FreshnessPolicy,
                request.PayloadSha256);
            string key = $"{mapping.Target.Type}:{mapping.Target.Id}";
            if (!latestByInternalTarget.TryGetValue(key, out LiveLatestObservation? current)
                || IsPreferred(latest, current))
            {
                latestByInternalTarget[key] = latest;
            }
        }

        foreach (LiveProviderDiagnostic diagnostic in request.Diagnostics)
        {
            incidents.Add(CreateIncident(
                request,
                null,
                LiveQualityIncidentReason.ProviderDiagnostic,
                diagnostic.Code,
                diagnostic.ExternalTargetId,
                diagnostic.Field,
                normalizedAtUtc,
                correlationId));
        }

        await this.incidentRepository.SaveAsync(incidents, cancellationToken);

        LiveLatestObservationWriteResult writeResult =
            await this.latestRepository.WriteLatestAsync(
                latestByInternalTarget.Values.ToArray(),
                cancellationToken);
        return new LiveLatestObservationIngestionResult(
            writeResult.InsertedCount + writeResult.UpdatedCount,
            writeResult.IgnoredCount,
            unmappedCount,
            ineligibleCount,
            invalidFreshnessCount,
            incidents.Count(static incident =>
                incident.Reason != LiveQualityIncidentReason.ProviderDiagnostic),
            request.Diagnostics.Count);
    }

    private static LiveQualityIncident CreateIncident(
        LiveLatestObservationIngestionRequest request,
        ExternalLiveObservation? observation,
        LiveQualityIncidentReason reason,
        string? diagnosticCode,
        string? diagnosticExternalTargetId,
        string? diagnosticField,
        DateTime detectedAtUtc,
        string correlationId)
    {
        return new LiveQualityIncident(
            Guid.NewGuid(),
            request.SourceId,
            observation,
            reason,
            diagnosticCode,
            diagnosticExternalTargetId,
            diagnosticField,
            request.ReceivedAtUtc,
            detectedAtUtc,
            detectedAtUtc.Add(IncidentRetention),
            correlationId,
            request.AdapterVersion,
            request.UsagePolicyVersion,
            request.TransformationVersion,
            request.Confidence,
            request.FreshnessPolicy,
            request.PayloadSha256);
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
