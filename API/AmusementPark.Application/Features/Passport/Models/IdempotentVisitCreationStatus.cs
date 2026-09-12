namespace AmusementPark.Application.Features.Passport.Models;

public enum IdempotentVisitCreationStatus
{
    Created = 1,
    Replayed = 2,
    Conflict = 3,
}
