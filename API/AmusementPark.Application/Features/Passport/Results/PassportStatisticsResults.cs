using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportItemExperienceResult(
    string VisitId,
    VisitDateResult Date);

public sealed record PassportItemRatingCoverageResult(
    long RatedRideCount,
    long TotalRideCount,
    double Rate);

public sealed record PassportRatingDistributionResult(
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
    PassportRatingDistributionResult? HistoricalRatings,
    double? CurrentGlobalRating,
    double? CurrentGlobalMinusHistoricalAverage,
    IReadOnlyCollection<PassportItemVisitStatisticsResult> ByVisit,
    IReadOnlyCollection<PassportItemYearStatisticsResult> ByYear,
    IReadOnlyCollection<PassportItemRatingPointResult> RatingTimeline,
    PassportRatingTrendResult? Trend);

public sealed record PassportItemVisitStatisticsResult(
    string VisitId,
    VisitDateResult Date,
    long RideCount,
    PassportItemRatingCoverageResult RatingCoverage,
    PassportRatingDistributionResult? HistoricalRatings);

public sealed record PassportItemYearStatisticsResult(
    int Year,
    long RideCount,
    long VisitCount,
    PassportItemRatingCoverageResult RatingCoverage,
    PassportRatingDistributionResult? HistoricalRatings);

public sealed record PassportItemRatingPointResult(
    string RideOccurrenceId,
    string VisitId,
    VisitDateResult Date,
    long SortPosition,
    double Rating);

public sealed record PassportRatingTrendResult(
    PassportRatingTrendKind Kind,
    long FirstWindowRatingCount,
    long LastWindowRatingCount,
    double FirstWindowAverage,
    double LastWindowAverage,
    double Delta);
