namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportGlobalParkActivityResult(
    string ParkId,
    string? ParkName,
    long VisitCount,
    long RecordedRideCount);
