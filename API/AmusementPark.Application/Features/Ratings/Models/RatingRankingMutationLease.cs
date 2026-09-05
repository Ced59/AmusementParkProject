using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RatingRankingMutationLease
{
    public RatingRankingMutationLease(
        RankingScopeKey scopeKey,
        string token)
    {
        if (!Guid.TryParseExact(token, "N", out Guid parsedToken))
        {
            throw new ArgumentException("The ranking mutation lease token is invalid.", nameof(token));
        }

        this.ScopeKey = scopeKey;
        this.Token = parsedToken.ToString("N");
    }

    public RankingScopeKey ScopeKey { get; }

    public string Token { get; }

    public static RatingRankingMutationLease Create(RankingScopeKey scopeKey)
    {
        return new RatingRankingMutationLease(
            scopeKey,
            Guid.NewGuid().ToString("N"));
    }
}
