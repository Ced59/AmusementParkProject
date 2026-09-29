using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Results;

public sealed record PublicLiveQueueResult(
    LiveQueueKind Kind,
    int? WaitTimeMinutes,
    bool IsEstimated,
    LiveQueueAvailability Availability,
    DateTime? ReturnStartUtc,
    DateTime? ReturnEndUtc,
    int? CurrentGroupStart,
    int? CurrentGroupEnd,
    DateTime? NextAllocationUtc,
    long? PriceMinorUnits,
    string? CurrencyCode);
