using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class RatingRankingSourceRevisionRepository : IRatingRankingSourceRevisionRepository
{
    private readonly IMongoCollection<RatingRankingSourceRevisionDocument> collection;
    private readonly TimeProvider timeProvider;

    public RatingRankingSourceRevisionRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);

        this.collection = database.GetCollection<RatingRankingSourceRevisionDocument>(
            settings.RatingRankingSourceRevisionsCollectionName);
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<RatingRankingSourceRevision> IncrementAsync(
        RankingScopeKey scopeKey,
        CancellationToken cancellationToken)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        FindOneAndUpdateOptions<RatingRankingSourceRevisionDocument> options =
            new FindOneAndUpdateOptions<RatingRankingSourceRevisionDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After,
            };
        RatingRankingSourceRevisionDocument? document = await this.collection.FindOneAndUpdateAsync(
            RatingRankingSourceRevisionMongoDefinitions.BuildScopeFilter(scopeKey),
            RatingRankingSourceRevisionMongoDefinitions.BuildIncrementUpdate(scopeKey, nowUtc),
            options,
            cancellationToken);
        if (document is null)
        {
            throw new InvalidOperationException(
                $"The source revision for ranking scope '{scopeKey.Value}' could not be incremented.");
        }

        return ToApplication(document);
    }

    public async Task<RatingRankingSourceRevision?> GetAsync(
        RankingScopeKey scopeKey,
        CancellationToken cancellationToken)
    {
        RatingRankingSourceRevisionDocument? document = await this.collection
            .Find(RatingRankingSourceRevisionMongoDefinitions.BuildScopeFilter(scopeKey))
            .FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToApplication(document);
    }

    private static RatingRankingSourceRevision ToApplication(
        RatingRankingSourceRevisionDocument document)
    {
        RankingScopeKey scopeKey = RankingScopeKey.Parse(document.ScopeKey);
        if (!string.Equals(document.Id, scopeKey.Value, StringComparison.Ordinal) ||
            document.Revision <= 0)
        {
            throw new InvalidOperationException("The persisted ranking source revision is invalid.");
        }

        return new RatingRankingSourceRevision(scopeKey, document.Revision, document.UpdatedAt);
    }
}
