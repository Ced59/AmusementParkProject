namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingRankingScheduledScopeResult(
    string ScopeKey,
    long RequestedSourceRevision);
