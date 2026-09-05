namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record PersonalRankingShareStatBucketResult(
    string Label,
    long Count,
    double AverageRating);
