using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class RatingRankingSourceRevisionMongoDefinitions
{
    public static FilterDefinition<RatingRankingSourceRevisionDocument> BuildScopeFilter(
        RankingScopeKey scopeKey)
    {
        return Builders<RatingRankingSourceRevisionDocument>.Filter.Eq(
            document => document.Id,
            scopeKey.Value);
    }

    public static UpdateDefinition<RatingRankingSourceRevisionDocument> BuildIncrementUpdate(
        RankingScopeKey scopeKey,
        DateTime nowUtc)
    {
        return Builders<RatingRankingSourceRevisionDocument>.Update
            .SetOnInsert(document => document.Id, scopeKey.Value)
            .SetOnInsert(document => document.CreatedAt, nowUtc)
            .Set(document => document.ScopeKey, scopeKey.Value)
            .Inc(document => document.Revision, 1)
            .Set(document => document.UpdatedAt, nowUtc);
    }
}
