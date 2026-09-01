using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class RatingRankingSourceRevisionRepositoryTests
{
    private static readonly DateTime NowUtc = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BuildBeginMutationUpdate_ShouldPersistAnOwnedLeaseWithoutAdvancingRevision()
    {
        RankingScopeKey scopeKey = RankingScopeKey.Parse("parks:global");
        RatingRankingMutationLease mutationLease = CreateLease(scopeKey, 1);
        DateTime leaseExpiresAtUtc = NowUtc.Add(
            RatingRankingSourceRevisionRepository.MutationLeaseDuration);

        BsonDocument filter = Render(
            RatingRankingSourceRevisionMongoDefinitions.BuildScopeFilter(scopeKey));
        BsonDocument update = Render(
            RatingRankingSourceRevisionMongoDefinitions.BuildBeginMutationUpdate(
                scopeKey,
                mutationLease,
                NowUtc,
                leaseExpiresAtUtc));

        Assert.Equal(scopeKey.Value, filter["_id"].AsString);
        Assert.Equal(scopeKey.Value, update["$setOnInsert"].AsBsonDocument["_id"].AsString);
        Assert.Equal(NowUtc, update["$setOnInsert"].AsBsonDocument["createdAt"].ToUniversalTime());
        Assert.Equal(scopeKey.Value, update["$set"].AsBsonDocument["scopeKey"].AsString);
        Assert.Equal(NowUtc, update["$set"].AsBsonDocument["updatedAt"].ToUniversalTime());
        Assert.Equal(
            leaseExpiresAtUtc,
            update["$set"].AsBsonDocument[$"mutationLeases.{mutationLease.Token}"].ToUniversalTime());
        Assert.False(update.Contains("$inc"));
        Assert.False(update.Contains("$min"));
    }

    [Fact]
    public async Task BeginMutationAsync_ShouldPersistThePendingLeaseWithoutExposingANewRevision()
    {
        RankingScopeKey scopeKey = RankingScopeKey.Parse("parks:global");
        Mock<IMongoCollection<RatingRankingSourceRevisionDocument>> collection =
            new Mock<IMongoCollection<RatingRankingSourceRevisionDocument>>(MockBehavior.Strict);
        collection
            .Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<RatingRankingSourceRevisionDocument>>(),
                It.IsAny<UpdateDefinition<RatingRankingSourceRevisionDocument>>(),
                It.Is<FindOneAndUpdateOptions<RatingRankingSourceRevisionDocument, RatingRankingSourceRevisionDocument>>(
                    options => options.IsUpsert && options.ReturnDocument == ReturnDocument.After),
                CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevisionDocument
            {
                Id = scopeKey.Value,
                ScopeKey = scopeKey.Value,
                Revision = 41,
                PendingMutationCount = 1,
                MutationLeaseExpiresAtUtc = NowUtc.Add(
                    RatingRankingSourceRevisionRepository.MutationLeaseDuration),
                CreatedAt = NowUtc,
                UpdatedAt = NowUtc,
            });
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database
            .Setup(value => value.GetCollection<RatingRankingSourceRevisionDocument>("scope-revisions", null))
            .Returns(collection.Object);
        RatingRankingSourceRevisionRepository repository = new RatingRankingSourceRevisionRepository(
            database.Object,
            new MongoDbSettings { RatingRankingSourceRevisionsCollectionName = "scope-revisions" },
            new FixedTimeProvider(NowUtc));

        RatingRankingMutationLease mutationLease = await repository.BeginMutationAsync(
            scopeKey,
            CancellationToken.None);

        Assert.Equal(scopeKey, mutationLease.ScopeKey);
        Assert.True(Guid.TryParseExact(mutationLease.Token, "N", out _));
        collection.VerifyAll();
        database.VerifyAll();
    }

    [Fact]
    public async Task CompleteMutationAsync_ShouldAtomicallyAdvanceRevisionBeforeRemovingLastLease()
    {
        RankingScopeKey scopeKey = RankingScopeKey.Parse("parks:global");
        RatingRankingMutationLease mutationLease = CreateLease(scopeKey, 1);
        FilterDefinition<RatingRankingSourceRevisionDocument>? capturedFilter = null;
        UpdateDefinition<RatingRankingSourceRevisionDocument>? capturedUpdate = null;
        Mock<IMongoCollection<RatingRankingSourceRevisionDocument>> collection =
            new Mock<IMongoCollection<RatingRankingSourceRevisionDocument>>(MockBehavior.Strict);
        collection
            .Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<RatingRankingSourceRevisionDocument>>(),
                It.IsAny<UpdateDefinition<RatingRankingSourceRevisionDocument>>(),
                It.Is<FindOneAndUpdateOptions<RatingRankingSourceRevisionDocument, RatingRankingSourceRevisionDocument>>(
                    options => !options.IsUpsert && options.ReturnDocument == ReturnDocument.After),
                CancellationToken.None))
            .Callback((
                FilterDefinition<RatingRankingSourceRevisionDocument> filter,
                UpdateDefinition<RatingRankingSourceRevisionDocument> update,
                FindOneAndUpdateOptions<RatingRankingSourceRevisionDocument, RatingRankingSourceRevisionDocument> _,
                CancellationToken _) =>
            {
                capturedFilter = filter;
                capturedUpdate = update;
            })
            .ReturnsAsync(new RatingRankingSourceRevisionDocument
            {
                Id = scopeKey.Value,
                ScopeKey = scopeKey.Value,
                Revision = 42,
                CreatedAt = NowUtc,
                UpdatedAt = NowUtc,
            });
        RatingRankingSourceRevisionRepository repository = CreateRepository(collection.Object);

        RatingRankingSourceRevision revision = await repository.CompleteMutationAsync(
            mutationLease,
            sourceChanged: true,
            CancellationToken.None);

        Assert.NotNull(capturedFilter);
        Assert.NotNull(capturedUpdate);
        BsonDocument filterDocument = Render(capturedFilter);
        BsonDocument updateDocument = Render(capturedUpdate);
        Assert.Equal(scopeKey.Value, filterDocument["_id"].AsString);
        Assert.True(filterDocument[$"mutationLeases.{mutationLease.Token}"].AsBsonDocument["$exists"].AsBoolean);
        Assert.True(updateDocument["$unset"].AsBsonDocument.Contains(
            $"mutationLeases.{mutationLease.Token}"));
        Assert.Equal(1, updateDocument["$inc"].AsBsonDocument["revision"].AsInt64);
        Assert.Equal(42, revision.Revision);
        Assert.True(revision.IsRebuildable);
        Assert.Null(revision.MutationLeaseExpiresAtUtc);
        collection.VerifyAll();
    }

    [Fact]
    public void BuildMutationLeaseFilter_ShouldNotAllowAnExpiredOwnerToSettleANewerLease()
    {
        RankingScopeKey scopeKey = RankingScopeKey.Parse("parks:global");
        RatingRankingMutationLease expiredLease = CreateLease(scopeKey, 1);
        RatingRankingMutationLease newerLease = CreateLease(scopeKey, 2);

        BsonDocument filter = Render(
            RatingRankingSourceRevisionMongoDefinitions.BuildMutationLeaseFilter(expiredLease));

        Assert.True(filter.Contains($"mutationLeases.{expiredLease.Token}"));
        Assert.False(filter.Contains($"mutationLeases.{newerLease.Token}"));
    }

    private static RatingRankingSourceRevisionRepository CreateRepository(
        IMongoCollection<RatingRankingSourceRevisionDocument> collection)
    {
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database
            .Setup(value => value.GetCollection<RatingRankingSourceRevisionDocument>("scope-revisions", null))
            .Returns(collection);
        return new RatingRankingSourceRevisionRepository(
            database.Object,
            new MongoDbSettings { RatingRankingSourceRevisionsCollectionName = "scope-revisions" },
            new FixedTimeProvider(NowUtc));
    }

    private static BsonDocument Render(
        FilterDefinition<RatingRankingSourceRevisionDocument> filter)
    {
        IBsonSerializer<RatingRankingSourceRevisionDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<RatingRankingSourceRevisionDocument>();
        RenderArgs<RatingRankingSourceRevisionDocument> arguments =
            new RenderArgs<RatingRankingSourceRevisionDocument>(serializer, BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }

    private static BsonDocument Render(
        UpdateDefinition<RatingRankingSourceRevisionDocument> update)
    {
        IBsonSerializer<RatingRankingSourceRevisionDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<RatingRankingSourceRevisionDocument>();
        RenderArgs<RatingRankingSourceRevisionDocument> arguments =
            new RenderArgs<RatingRankingSourceRevisionDocument>(serializer, BsonSerializer.SerializerRegistry);
        return update.Render(arguments).AsBsonDocument;
    }

    private static RatingRankingMutationLease CreateLease(
        RankingScopeKey scopeKey,
        int tokenSeed)
    {
        return new RatingRankingMutationLease(
            scopeKey,
            tokenSeed.ToString("x32"));
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset now;

        public FixedTimeProvider(DateTime nowUtc)
        {
            this.now = new DateTimeOffset(nowUtc);
        }

        public override DateTimeOffset GetUtcNow()
        {
            return this.now;
        }
    }
}
