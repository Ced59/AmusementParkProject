namespace AmusementPark.Core.Domain.Trips;

public enum TripPlanWriteOutcome
{
    Success = 1,
    NotFound = 2,
    Conflict = 3,
    LimitReached = 4,
}
