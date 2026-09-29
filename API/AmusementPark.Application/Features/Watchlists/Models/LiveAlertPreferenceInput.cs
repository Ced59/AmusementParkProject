using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Models;

public sealed record LiveAlertPreferenceInput(
    string TargetId,
    LiveAlertType Type,
    int? ThresholdMinutes,
    int DurationMinutes);
