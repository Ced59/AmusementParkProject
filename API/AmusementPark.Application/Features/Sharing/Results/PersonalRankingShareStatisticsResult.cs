namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record PersonalRankingShareStatisticsResult(
    long TotalRatings,
    double AverageRating,
    double HighestRating,
    double LowestRating,
    IReadOnlyCollection<PersonalRankingShareStatBucketResult> ByPark,
    IReadOnlyCollection<PersonalRankingShareStatBucketResult> ByTargetType,
    IReadOnlyCollection<PersonalRankingShareStatBucketResult> ByParkItemCategory);
