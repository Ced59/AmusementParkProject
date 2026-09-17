using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record IdempotentTripPlanCreationResult(
    IdempotentTripPlanCreationStatus Status,
    TripPlan? TripPlan);
