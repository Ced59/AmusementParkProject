namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record YearRecapShareHighlightResult(
    string Name,
    long RideCount,
    long RatingCount,
    double? AverageRating,
    bool IsNowClosed);
