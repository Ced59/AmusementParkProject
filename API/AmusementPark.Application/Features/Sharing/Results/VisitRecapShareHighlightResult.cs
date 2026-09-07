namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record VisitRecapShareHighlightResult(
    string Name,
    int? RideCount,
    double? Rating);
