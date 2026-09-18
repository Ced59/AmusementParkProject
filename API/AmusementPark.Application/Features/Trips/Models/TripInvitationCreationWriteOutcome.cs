namespace AmusementPark.Application.Features.Trips.Models;

public enum TripInvitationCreationWriteOutcome
{
    Success = 1,
    IdempotencyConflict = 2,
    LimitReached = 3,
    TokenCollision = 4,
    LeaseExpired = 5,
}
