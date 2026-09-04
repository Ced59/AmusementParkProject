namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportItemExperienceResult(
    string VisitId,
    VisitDateResult Date);

public sealed record PassportItemRatingCoverageResult(
    long RatedRideCount,
    long TotalRideCount,
    double Rate);

public sealed record PassportItemHistoricalRatingsResult(
    long RatingCount,
    double Average,
    double Median,
    double Minimum,
    double Maximum,
    double PopulationStandardDeviation);

public sealed record PassportItemStatisticsResult(
    string ParkItemId,
    long RideCount,
    long VisitCount,
    PassportItemRatingCoverageResult RatingCoverage,
    PassportItemExperienceResult? FirstExperience,
    PassportItemExperienceResult? LastExperience,
    PassportItemHistoricalRatingsResult? HistoricalRatings,
    double? CurrentGlobalRating,
    double? CurrentGlobalMinusHistoricalAverage);
