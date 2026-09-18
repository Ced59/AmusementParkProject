namespace AmusementPark.Application.Features.Trips.Models;

public enum TripInvitationWriteOutcome
{
    Success = 1,
    NotFound = 2,
    Conflict = 3,
    LeaseExpired = 4,
}
