namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportHistoricalNameResult(
    string? ParkName,
    string NameAtVisit,
    string? CurrentName,
    int FirstVisitYear,
    int LastVisitYear,
    long CompletedRideCount);
