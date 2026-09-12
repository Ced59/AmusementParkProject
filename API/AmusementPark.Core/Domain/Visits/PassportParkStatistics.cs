using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportParkStatistics(
    string ParkId,
    PassportStatisticsSummary Summary,
    RatingValue? CurrentGlobalRating,
    double? CurrentGlobalMinusHistoricalAverage,
    IReadOnlyCollection<PassportParkAssessmentPoint> AssessmentTimeline,
    IReadOnlyCollection<PassportYearBreakdown> ByYear,
    IReadOnlyCollection<PassportCurrentItemRating> CurrentTopItems,
    IReadOnlyCollection<PassportHistoricalItemRating> HistoricalTopItems);
