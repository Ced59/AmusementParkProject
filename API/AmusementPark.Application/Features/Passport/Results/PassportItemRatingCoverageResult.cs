namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportItemRatingCoverageResult(
    long RatedRideCount,
    long TotalRideCount,
    double Rate);
