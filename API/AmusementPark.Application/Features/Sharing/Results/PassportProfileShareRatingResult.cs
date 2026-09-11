namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record PassportProfileShareRatingResult(
    string TargetType,
    string Name,
    string? ParkName,
    string? Category,
    double Rating);
