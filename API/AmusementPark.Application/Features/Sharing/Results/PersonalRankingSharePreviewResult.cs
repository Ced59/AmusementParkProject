namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record PersonalRankingSharePreviewResult(
    string? DisplayName,
    string? AvatarUrl,
    PersonalRankingShareStatisticsResult? Statistics,
    IReadOnlyCollection<PersonalRankingSharePreviewItemResult> Ratings,
    bool IsTruncated);
