namespace AmusementPark.Application.Features.Trips.Models;

public enum IdempotentTripPlanCreationStatus
{
    Created = 1,
    Replayed = 2,
    Conflict = 3,
    LimitReached = 4,
    Deleted = 5,
}
