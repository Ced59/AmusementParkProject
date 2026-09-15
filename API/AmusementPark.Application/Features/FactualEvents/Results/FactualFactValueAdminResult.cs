using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.FactualEvents.Results;

public sealed record FactualFactValueAdminResult(
    FactValueKind Kind,
    string CanonicalValue,
    string? UnitCode);
