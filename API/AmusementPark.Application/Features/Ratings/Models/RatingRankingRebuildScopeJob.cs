using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public static class RatingRankingRebuildScopeJob
{
    public const string Kind = "ratings.rebuild-scope";
    public const int PayloadVersion = 1;

    public static string BuildNaturalKey(RankingScopeKey scopeKey)
    {
        return $"{Kind}:{scopeKey.Value}";
    }

    public static string BuildForcedNaturalKey(RankingScopeKey scopeKey)
    {
        return $"{BuildNaturalKey(scopeKey)}:forced";
    }
}
