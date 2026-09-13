namespace AmusementPark.Core.Domain.Sharing;

public sealed record ProfileComparisonRatingData(
    string TargetType,
    string Name,
    string? ParkName,
    string? Category,
    double Rating);
