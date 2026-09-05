namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingRankingSourceBatch(
    IReadOnlyCollection<RatingRankingItemResult> Sources,
    bool IsTruncated);
