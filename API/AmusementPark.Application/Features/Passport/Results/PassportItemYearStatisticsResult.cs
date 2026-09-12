namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportItemYearStatisticsResult(
    int Year,
    long RideCount,
    long VisitCount,
    PassportItemRatingCoverageResult RatingCoverage,
    PassportRatingDistributionResult? HistoricalRatings);
