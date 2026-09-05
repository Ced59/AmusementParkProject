namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RatingRankingPolicyEvaluationPlan(
    int TotalEntryCount,
    IReadOnlyCollection<RatingRankingPolicyEvaluationEntry> Entries,
    bool IsSourceTruncated);
