using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.Watchlists.Results;

public sealed record UserNotificationFactValueResult(
    FactValueKind Kind,
    string CanonicalValue,
    string? UnitCode);
