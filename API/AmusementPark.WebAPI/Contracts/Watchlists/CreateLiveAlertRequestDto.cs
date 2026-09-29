namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed record CreateLiveAlertRequestDto(
    string TargetId,
    string Type,
    int? ThresholdMinutes,
    int DurationMinutes);
