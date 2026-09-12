using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportItemStatistics(
    long RideCount,
    long VisitCount,
    long RatedRideCount,
    double RatingCoverageRate,
    PassportItemExperience? FirstExperience,
    PassportItemExperience? LastExperience,
    PassportRatingStatistics? Ratings,
    RatingValue? CurrentGlobalRating,
    double? CurrentGlobalMinusHistoricalAverage,
    IReadOnlyCollection<PassportItemVisitStatistics> ByVisit,
    IReadOnlyCollection<PassportItemYearStatistics> ByYear,
    IReadOnlyCollection<PassportItemRatingPoint> RatingTimeline,
    PassportRatingTrend? Trend);
