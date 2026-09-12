namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportItemVisitStatisticsResult(
    string VisitId,
    VisitDateResult Date,
    long RideCount,
    PassportItemRatingCoverageResult RatingCoverage,
    PassportRatingDistributionResult? HistoricalRatings);
