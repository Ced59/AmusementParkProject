namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportHistoricalParkEraResult(
    string? ParkName,
    int FirstVisitYear,
    int LastVisitYear,
    long VisitCount,
    long CanonicalEraCount);
