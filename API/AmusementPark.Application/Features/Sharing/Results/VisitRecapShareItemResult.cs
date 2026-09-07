namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record VisitRecapShareItemResult(
    string ParkItemId,
    string Name,
    string? Category,
    int? RideCount,
    double? AverageRating,
    bool IsMissed);
