namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingRankingRebuildRequestResult(
    DateTime RequestedAtUtc,
    int ScheduledScopeCount,
    IReadOnlyCollection<RatingRankingScheduledScopeResult> Scopes);
