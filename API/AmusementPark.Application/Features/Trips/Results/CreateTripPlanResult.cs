namespace AmusementPark.Application.Features.Trips.Results;

public sealed record CreateTripPlanResult(TripPlanResult TripPlan, bool WasReplayed);
