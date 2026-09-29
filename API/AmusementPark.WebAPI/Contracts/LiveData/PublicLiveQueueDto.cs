namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed record PublicLiveQueueDto(
    string Kind,
    int? WaitTimeMinutes,
    bool IsEstimated,
    string Availability,
    DateTime? ReturnStartUtc,
    DateTime? ReturnEndUtc,
    int? CurrentGroupStart,
    int? CurrentGroupEnd,
    DateTime? NextAllocationUtc,
    long? PriceMinorUnits,
    string? CurrencyCode);
