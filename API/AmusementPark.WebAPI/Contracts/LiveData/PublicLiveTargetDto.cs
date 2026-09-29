namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed record PublicLiveTargetDto(
    string TargetId,
    string TargetType,
    string DisplayName,
    string ParkId,
    string ParkDisplayName,
    string Availability,
    string? Status,
    IReadOnlyCollection<PublicLiveQueueDto> Queues,
    DateTime AsOfUtc,
    DateTime? ObservedAtUtc,
    DateTime? ReceivedAtUtc,
    long? AgeSeconds,
    string? Freshness,
    DateTime? ExpiresAtUtc,
    PublicLiveSourceDto? Source,
    string? Confidence);
