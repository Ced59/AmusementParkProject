namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed record LiveAlertSubscriptionDto(
    string SubscriptionId,
    string TargetId,
    string ParkId,
    string? TargetName,
    string? ParkName,
    string? MainImageId,
    string Type,
    int? ThresholdMinutes,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? LastObservedAtUtc,
    string? LastStatus,
    int? LastWaitMinutes,
    int CooldownMinutes,
    int HysteresisMinutes,
    long Version);
