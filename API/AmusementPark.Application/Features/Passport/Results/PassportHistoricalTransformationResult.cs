namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportHistoricalTransformationResult(
    string? ParkName,
    string CurrentName,
    string CurrentCategory,
    IReadOnlyCollection<string> NamesAtVisit,
    IReadOnlyCollection<string> CategoriesAtVisit,
    int FirstVisitYear,
    int LastVisitYear,
    long CompletedRideCount);
