namespace AmusementPark.Core.Domain.Ratings;

public sealed record GlobalRatingSuggestionEvaluation(
    GlobalRatingSuggestionReason Reason,
    int NewObservationCount,
    int RecentObservationCount,
    RatingValue LatestObservation,
    double RecentAverage,
    double HistoricalMedian,
    DateTime LatestObservationAtUtc);
