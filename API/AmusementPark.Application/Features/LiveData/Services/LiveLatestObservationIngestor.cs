using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class LiveLatestObservationIngestor : ILiveLatestObservationIngestor
{
    private readonly ILiveTargetMappingRepository mappingRepository;
    private readonly ILiveLatestObservationRepository latestRepository;
    private readonly TimeProvider timeProvider;

    public LiveLatestObservationIngestor(
        ILiveTargetMappingRepository mappingRepository,
        ILiveLatestObservationRepository latestRepository)
        : this(mappingRepository, latestRepository, TimeProvider.System)
    {
    }

    internal LiveLatestObservationIngestor(
        ILiveTargetMappingRepository mappingRepository,
        ILiveLatestObservationRepository latestRepository,
        TimeProvider timeProvider)
    {
        this.mappingRepository = mappingRepository
            ?? throw new ArgumentNullException(nameof(mappingRepository));
        this.latestRepository = latestRepository
            ?? throw new ArgumentNullException(nameof(latestRepository));
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
        int unmappedCount = 0;
        int ineligibleCount = 0;
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
                continue;
            }

            if (!mapping.IsEligibleForLiveUse
                || mapping.Target is null
                || mapping.ExternalTarget.Type != observation.TargetType
                || mapping.Target.Type != observation.TargetType)
            {
                ineligibleCount++;
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

        LiveLatestObservationWriteResult writeResult =
            await this.latestRepository.WriteLatestAsync(
                latestByInternalTarget.Values.ToArray(),
                cancellationToken);
        return new LiveLatestObservationIngestionResult(
            writeResult.InsertedCount + writeResult.UpdatedCount,
            writeResult.IgnoredCount,
            unmappedCount,
            ineligibleCount);
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
