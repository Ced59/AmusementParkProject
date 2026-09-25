using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripExportDecisionResult(
    string? ParkName,
    string? ParkItemName,
    bool IsParkItemAvailable,
    TripItemDecisionStatus Status,
    string Reason,
    DateTime DecidedAtUtc);
