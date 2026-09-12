namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportItemStatisticsResult(
    string ParkItemId,
    long RideCount,
    long VisitCount,
    PassportItemRatingCoverageResult RatingCoverage,
    PassportItemExperienceResult? FirstExperience,
    PassportItemExperienceResult? LastExperience,
    PassportRatingDistributionResult? HistoricalRatings,
    double? CurrentGlobalRating,
    double? CurrentGlobalMinusHistoricalAverage,
    IReadOnlyCollection<PassportItemVisitStatisticsResult> ByVisit,
    IReadOnlyCollection<PassportItemYearStatisticsResult> ByYear,
    IReadOnlyCollection<PassportItemRatingPointResult> RatingTimeline,
    PassportRatingTrendResult? Trend,
    string? ParkItemName = null);
