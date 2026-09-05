namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record UserRankingSharePreviewItemResult(
    int Rank,
    string Name,
    string? ParkName,
    double Rating);
