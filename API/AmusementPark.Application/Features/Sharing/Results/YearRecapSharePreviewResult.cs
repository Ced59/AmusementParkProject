namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record YearRecapSharePreviewResult(
    int Year,
    long? ParkCount,
    long VisitCount,
    long ApproximateVisitCount,
    double ApproximateVisitRate,
    long? TotalRideCount,
    long? DistinctItemCount,
    long? MissedItemCount,
    IReadOnlyCollection<string> Categories,
    YearRecapShareRatingSummaryResult? ParkRatings,
    YearRecapShareRatingSummaryResult? RideRatings,
    IReadOnlyCollection<YearRecapShareParkResult> MostVisitedParks,
    YearRecapShareHighlightResult? MostRepeatedItem,
    YearRecapShareHighlightResult? TopRatedItem,
    YearRecapShareTrendResult? RatingEvolution,
    IReadOnlyCollection<YearRecapShareHighlightResult> NowClosedItems,
    string? PublicCaption,
    bool HasIncompleteCatalog,
    string CalculationVersion,
    bool IsEmpty);
