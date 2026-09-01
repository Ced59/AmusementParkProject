using AmusementPark.Application.Features.Ratings.Models;
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

    public static UpdateDefinition<RatingRankingSourceRevisionDocument> BuildBeginMutationUpdate(
        RankingScopeKey scopeKey,
        RatingRankingMutationLease mutationLease,
        DateTime nowUtc,
        DateTime leaseExpiresAtUtc)
    {
        EnsureScopeMatches(scopeKey, mutationLease);
        return Builders<RatingRankingSourceRevisionDocument>.Update
            .SetOnInsert(document => document.Id, scopeKey.Value)
            .SetOnInsert(document => document.CreatedAt, nowUtc)
            .Set(document => document.ScopeKey, scopeKey.Value)
            .Set(BuildMutationLeaseField(mutationLease.Token), leaseExpiresAtUtc)
            .Set(document => document.UpdatedAt, nowUtc);
    }

    public static FilterDefinition<RatingRankingSourceRevisionDocument> BuildMutationLeaseFilter(
        RatingRankingMutationLease mutationLease)
    {
        return BuildScopeFilter(mutationLease.ScopeKey)
            & Builders<RatingRankingSourceRevisionDocument>.Filter.Exists(
                BuildMutationLeaseField(mutationLease.Token));
    }

    public static FilterDefinition<RatingRankingSourceRevisionDocument> BuildExpiredMutationLeaseFilter(
        RatingRankingMutationLease mutationLease,
        DateTime nowUtc)
    {
        return BuildScopeFilter(mutationLease.ScopeKey)
            & Builders<RatingRankingSourceRevisionDocument>.Filter.Lte(
                BuildMutationLeaseField(mutationLease.Token),
                nowUtc);
    }

    public static UpdateDefinition<RatingRankingSourceRevisionDocument> BuildCompleteMutationUpdate(
        RatingRankingMutationLease mutationLease,
        bool sourceChanged,
        DateTime nowUtc)
    {
        UpdateDefinition<RatingRankingSourceRevisionDocument> update =
            Builders<RatingRankingSourceRevisionDocument>.Update
                .Unset(BuildMutationLeaseField(mutationLease.Token))
                .Set(document => document.UpdatedAt, nowUtc);
        return sourceChanged
            ? update.Inc(document => document.Revision, 1)
            : update;
    }

    public static UpdateDefinition<RatingRankingSourceRevisionDocument> BuildLateChangedMutationUpdate(
        DateTime nowUtc)
    {
        return Builders<RatingRankingSourceRevisionDocument>.Update
            .Inc(document => document.Revision, 1)
            .Set(document => document.UpdatedAt, nowUtc);
    }

    public static UpdateDefinition<RatingRankingSourceRevisionDocument> BuildRecoverMutationUpdate(
        RatingRankingMutationLease mutationLease,
        DateTime nowUtc)
    {
        return Builders<RatingRankingSourceRevisionDocument>.Update
            .Unset(BuildMutationLeaseField(mutationLease.Token))
            .Inc(document => document.Revision, 1)
            .Set(document => document.UpdatedAt, nowUtc);
    }

    public static FilterDefinition<RatingRankingSourceRevisionDocument> BuildPendingMutationFilter(
        RankingScopeKey scopeKey)
    {
        return BuildScopeFilter(scopeKey)
            & Builders<RatingRankingSourceRevisionDocument>.Filter.Gt(
                document => document.PendingMutationCount,
                0);
    }

    private static string BuildMutationLeaseField(string token)
    {
        if (!Guid.TryParseExact(token, "N", out _))
        {
            throw new ArgumentException("The ranking mutation lease token is invalid.", nameof(token));
        }

        return $"mutationLeases.{token}";
    }

    private static void EnsureScopeMatches(
        RankingScopeKey scopeKey,
        RatingRankingMutationLease mutationLease)
    {
        if (scopeKey != mutationLease.ScopeKey)
        {
            throw new ArgumentException(
                "The ranking mutation lease belongs to another scope.",
                nameof(mutationLease));
        }
    }
}
