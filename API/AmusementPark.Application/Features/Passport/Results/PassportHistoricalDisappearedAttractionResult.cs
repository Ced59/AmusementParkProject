namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportHistoricalDisappearedAttractionResult(
    string? ParkName,
    string AttractionName,
    int FirstVisitYear,
    int LastVisitYear,
    long CompletedRideCount);
