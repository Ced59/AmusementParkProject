namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record UserRankingSharePreviewResult(
    string DisplayName,
    IReadOnlyCollection<UserRankingSharePreviewItemResult> Items);
