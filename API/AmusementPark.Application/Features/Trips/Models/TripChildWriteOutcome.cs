namespace AmusementPark.Application.Features.Trips.Models;

public enum TripChildWriteOutcome
{
    Success = 1,
    NotFound = 2,
    Conflict = 3,
    Duplicate = 4,
    LeaseExpired = 5,
}
