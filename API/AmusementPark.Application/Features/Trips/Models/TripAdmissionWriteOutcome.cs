namespace AmusementPark.Application.Features.Trips.Models;

public enum TripAdmissionWriteOutcome
{
    Success = 1,
    AlreadyCompleted = 2,
    NotFound = 3,
    Conflict = 4,
    AdmissionsClosed = 5,
    MemberLimitReached = 6,
}
