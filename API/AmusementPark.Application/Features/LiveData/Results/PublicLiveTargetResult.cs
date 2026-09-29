using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Results;

public sealed record PublicLiveTargetResult(
    string TargetId,
    LiveTargetType TargetType,
    string DisplayName,
    string ParkId,
    string ParkDisplayName,
    PublicLiveAvailability Availability,
    LiveOperationalStatus? Status,
    IReadOnlyCollection<PublicLiveQueueResult> Queues,
    DateTime AsOfUtc,
    DateTime? ObservedAtUtc,
    DateTime? ReceivedAtUtc,
    long? AgeSeconds,
    LiveFreshnessState? Freshness,
    DateTime? ExpiresAtUtc,
    DateTime? FreshnessTransitionAtUtc,
    PublicLiveSourceResult? Source,
    LiveDataConfidence? Confidence);
