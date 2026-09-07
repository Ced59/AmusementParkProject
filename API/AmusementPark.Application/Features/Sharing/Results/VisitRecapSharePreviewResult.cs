namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record VisitRecapSharePreviewResult(
    string ParkId,
    string? ParkName,
    VisitRecapShareDateResult? Date,
    int? DistinctItemCount,
    int? TotalRideCount,
    IReadOnlyCollection<string> Categories,
    double? ParkRating,
    VisitRecapShareHighlightResult? TopRatedItem,
    VisitRecapShareHighlightResult? MostRepeatedItem,
    IReadOnlyCollection<VisitRecapShareItemResult> Items,
    string? PublicCaption,
    bool HasHiddenDate,
    bool HasIncompleteRatings,
    bool HasIncompleteItems);
