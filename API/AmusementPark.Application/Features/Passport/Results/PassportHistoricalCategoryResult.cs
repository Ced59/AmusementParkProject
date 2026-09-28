namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportHistoricalCategoryResult(
    string Category,
    long CompletedRideCount,
    long DistinctAttractionCount);
