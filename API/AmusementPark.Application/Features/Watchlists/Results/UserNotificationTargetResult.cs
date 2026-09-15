using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.Watchlists.Results;

public sealed record UserNotificationTargetResult(
    FactualTargetType Type,
    string TargetId,
    string? ParkId,
    string? Name,
    string? ParentParkName,
    string? MainImageId);
