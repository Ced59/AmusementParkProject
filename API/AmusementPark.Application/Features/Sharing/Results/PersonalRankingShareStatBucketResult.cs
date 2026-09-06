namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record PersonalRankingShareStatBucketResult(
    string? Key,
    string Label,
    long Count,
    double AverageRating);
