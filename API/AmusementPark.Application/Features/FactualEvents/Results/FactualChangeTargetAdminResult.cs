using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.FactualEvents.Results;

public sealed record FactualChangeTargetAdminResult(
    FactualTargetType Type,
    string TargetId,
    string? TargetName,
    string? ParentParkId,
    string? ParentParkName);
