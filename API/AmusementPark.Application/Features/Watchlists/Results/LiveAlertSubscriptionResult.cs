using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Results;

public sealed record LiveAlertSubscriptionResult(
    string SubscriptionId,
    string TargetId,
    string ParkId,
    string? TargetName,
    string? ParkName,
    string? MainImageId,
    LiveAlertType Type,
    int? ThresholdMinutes,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? LastObservedAtUtc,
    LiveOperationalStatus? LastStatus,
    int? LastWaitMinutes,
    int CooldownMinutes,
    int HysteresisMinutes,
    long Version);
