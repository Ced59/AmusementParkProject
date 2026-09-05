namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportGlobalYearActivityResult(
    int Year,
    long VisitCount,
    long RecordedRideCount);
