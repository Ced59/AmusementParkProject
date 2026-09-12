namespace AmusementPark.Application.Features.Passport.Models;

public enum IdempotentRideOccurrenceReorderStatus
{
    Applied = 1,
    Replayed = 2,
    Conflict = 3,
    IdempotencyConflict = 4,
}
