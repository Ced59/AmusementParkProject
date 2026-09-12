namespace AmusementPark.Application.Features.Passport.Models;

public enum IdempotentRideOccurrenceCreationStatus
{
    Created = 1,
    Replayed = 2,
    Conflict = 3,
    ConcurrencyConflict = 4,
}
