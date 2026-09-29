using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Models;

public sealed record LiveLatestObservationIngestionRequest(
    LiveDataSourceId SourceId,
    string AdapterVersion,
    string UsagePolicyVersion,
    string TransformationVersion,
    LiveDataConfidence Confidence,
    LiveFreshnessPolicy FreshnessPolicy,
    DateTime ReceivedAtUtc,
    IReadOnlyCollection<ExternalLiveObservation> Observations,
    string? PayloadSha256);
