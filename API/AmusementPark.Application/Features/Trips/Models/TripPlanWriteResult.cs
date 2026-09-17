using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripPlanWriteResult(
    TripPlanWriteOutcome Outcome,
    long? CurrentVersion,
    TripPlan? PersistedTripPlan = null);
