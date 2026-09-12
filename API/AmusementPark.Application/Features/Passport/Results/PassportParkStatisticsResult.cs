namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportParkStatisticsResult(
    string ParkId,
    PassportStatisticsSummaryResult Summary,
    double? CurrentGlobalRating,
    double? CurrentGlobalMinusHistoricalAverage,
    IReadOnlyCollection<PassportParkAssessmentPointResult> AssessmentTimeline,
    IReadOnlyCollection<PassportYearBreakdownResult> ByYear,
    IReadOnlyCollection<PassportCurrentItemRatingResult> CurrentTopItems,
    IReadOnlyCollection<PassportHistoricalItemRatingResult> HistoricalTopItems,
    string? ParkName = null);
