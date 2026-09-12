namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportRatingCoverageResult(
    long RatedCount,
    long TotalCount,
    double Rate);
