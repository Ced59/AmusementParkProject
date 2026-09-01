using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RatingRankingSourceRevision(
    RankingScopeKey ScopeKey,
    long Revision,
    DateTime UpdatedAtUtc);

public sealed record RatingRankingRebuildJobPayload(
    string ScopeKey,
    long RequestedSourceRevision,
    string MethodologyVersion);

public static class RatingRankingRebuildJobContract
{
    public const string Kind = "ratings.rebuild-scope";

    public const int PayloadVersion = 1;

    public static string CreateNaturalKey(RankingScopeKey scopeKey)
    {
        return $"{Kind}:{scopeKey.Value}";
    }
}
