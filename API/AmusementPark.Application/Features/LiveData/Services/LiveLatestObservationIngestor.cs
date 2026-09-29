using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class LiveLatestObservationIngestor : ILiveLatestObservationIngestor
{
    private static readonly TimeSpan IncidentRetention = TimeSpan.FromDays(7);
    private readonly ILiveTargetMappingRepository mappingRepository;
    private readonly ILiveLatestObservationRepository latestRepository;
    private readonly ILiveQualityIncidentRepository incidentRepository;
    private readonly ILiveOperationalGate operationalGate;
    private readonly LiveOperationalObservationWriter operationalWriter;
    private readonly LiveAlertEvaluationService? liveAlertEvaluationService;
    private readonly TimeProvider timeProvider;

    internal LiveLatestObservationIngestor(
        ILiveTargetMappingRepository mappingRepository,
        ILiveLatestObservationRepository latestRepository,
        ILiveQualityIncidentRepository incidentRepository,
        ILiveOperationalGate operationalGate)
        : this(
            mappingRepository,
            latestRepository,
            incidentRepository,
            operationalGate,
            new LiveOperationalWriteCoordinator(),
            TimeProvider.System,
            null)
    {
    }

    public LiveLatestObservationIngestor(
        ILiveTargetMappingRepository mappingRepository,
        ILiveLatestObservationRepository latestRepository,
        ILiveQualityIncidentRepository incidentRepository,
        ILiveOperationalGate operationalGate,
        LiveOperationalWriteCoordinator coordinator)
        : this(
            mappingRepository,
            latestRepository,
            incidentRepository,
            operationalGate,
            coordinator,
            TimeProvider.System,
            null)
    {
    }

    public LiveLatestObservationIngestor(
        ILiveTargetMappingRepository mappingRepository,
        ILiveLatestObservationRepository latestRepository,
        ILiveQualityIncidentRepository incidentRepository,
        ILiveOperationalGate operationalGate,
        LiveOperationalWriteCoordinator coordinator,
        LiveAlertEvaluationService liveAlertEvaluationService)
        : this(
            mappingRepository,
            latestRepository,
            incidentRepository,
            operationalGate,
            coordinator,
            TimeProvider.System,
            liveAlertEvaluationService)
    {
    }

    internal LiveLatestObservationIngestor(
        ILiveTargetMappingRepository mappingRepository,
        ILiveLatestObservationRepository latestRepository,
        ILiveQualityIncidentRepository incidentRepository,
        ILiveOperationalGate operationalGate,
        TimeProvider timeProvider)
        : this(
            mappingRepository,
            latestRepository,
            incidentRepository,
            operationalGate,
            new LiveOperationalWriteCoordinator(),
            timeProvider,
            null)
    {
    }

    internal LiveLatestObservationIngestor(
        ILiveTargetMappingRepository mappingRepository,
        ILiveLatestObservationRepository latestRepository,
        ILiveQualityIncidentRepository incidentRepository,
        ILiveOperationalGate operationalGate,
        LiveOperationalWriteCoordinator coordinator,
        TimeProvider timeProvider,
        LiveAlertEvaluationService? liveAlertEvaluationService)
    {
        this.mappingRepository = mappingRepository
            ?? throw new ArgumentNullException(nameof(mappingRepository));
        this.latestRepository = latestRepository
            ?? throw new ArgumentNullException(nameof(latestRepository));
        this.incidentRepository = incidentRepository
            ?? throw new ArgumentNullException(nameof(incidentRepository));
        this.operationalGate = operationalGate
            ?? throw new ArgumentNullException(nameof(operationalGate));
        this.operationalWriter = new LiveOperationalObservationWriter(
            this.latestRepository,
            this.operationalGate,
            coordinator);
        this.liveAlertEvaluationService = liveAlertEvaluationService;
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
        string externalEntityId = mappings
            .Select(static mapping => mapping.ExternalTarget.Type == LiveTargetType.Park
                ? mapping.ExternalTarget.Id
                : mapping.ExternalTarget.ParentId)
            .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))
            ?? request.Observations
                .FirstOrDefault(static observation => observation.TargetType == LiveTargetType.Park)
                ?.ExternalTargetId
            ?? string.Empty;
        LiveOperationalGateSnapshot? operationalGate = externalEntityId.Length == 0
            ? null
            : await this.operationalGate.LoadAsync(
                request.SourceId,
                externalEntityId,
                cancellationToken);
        Dictionary<string, LiveLatestObservation> latestByInternalTarget =
            new Dictionary<string, LiveLatestObservation>(StringComparer.Ordinal);
        List<LiveQualityIncident> incidents = new List<LiveQualityIncident>();
        int unmappedCount = 0;
        int ineligibleCount = 0;
        int suppressedByOperationalControlCount = 0;
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

            if (operationalGate is null
                || !operationalGate.AllowsCollection(
                    mapping.Target.ParkId,
                    mapping.Target.Type,
                    mapping.Target.Id))
            {
                suppressedByOperationalControlCount++;
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

        LiveLatestObservationWriteResult writeResult;
        int suppressedAtWriteBoundary = 0;
        IReadOnlyCollection<LiveLatestObservation> committedAlertObservations =
            Array.Empty<LiveLatestObservation>();
        if (externalEntityId.Length == 0)
        {
            writeResult = await this.latestRepository.WriteLatestAsync(
                latestByInternalTarget.Values.ToArray(),
                cancellationToken);
        }
        else
        {
            LiveOperationalObservationWriteResult operationalWrite =
                await this.operationalWriter.WriteAsync(
                    request.SourceId,
                    externalEntityId,
                    latestByInternalTarget.Values.ToArray(),
                    cancellationToken);
            writeResult = operationalWrite.WriteResult;
            committedAlertObservations = writeResult.CommittedObservations;
            suppressedAtWriteBoundary = latestByInternalTarget.Count
                - operationalWrite.AcceptedObservations.Count;
        }

        if (this.liveAlertEvaluationService is not null && committedAlertObservations.Count > 0)
        {
            await this.liveAlertEvaluationService.EvaluateAsync(
                committedAlertObservations,
                cancellationToken);
        }

        return new LiveLatestObservationIngestionResult(
            writeResult.InsertedCount + writeResult.UpdatedCount,
            writeResult.IgnoredCount,
            unmappedCount,
            ineligibleCount,
            suppressedByOperationalControlCount + suppressedAtWriteBoundary,
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
