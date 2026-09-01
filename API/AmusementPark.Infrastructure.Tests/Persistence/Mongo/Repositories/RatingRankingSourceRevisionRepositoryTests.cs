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
    public void BuildBeginMutationUpdate_ShouldHideTheRevisionBehindADurableLease()
    {
        RankingScopeKey scopeKey = RankingScopeKey.Parse("parks:global");
        DateTime leaseExpiresAtUtc = NowUtc.Add(
            RatingRankingSourceRevisionRepository.MutationLeaseDuration);

        BsonDocument filter = Render(
            RatingRankingSourceRevisionMongoDefinitions.BuildScopeFilter(scopeKey));
        BsonDocument update = Render(
            RatingRankingSourceRevisionMongoDefinitions.BuildBeginMutationUpdate(
                scopeKey,
                NowUtc,
                leaseExpiresAtUtc));

        Assert.Equal(scopeKey.Value, filter["_id"].AsString);
        Assert.Equal(scopeKey.Value, update["$setOnInsert"].AsBsonDocument["_id"].AsString);
        Assert.Equal(NowUtc, update["$setOnInsert"].AsBsonDocument["createdAt"].ToUniversalTime());
        Assert.Equal(scopeKey.Value, update["$set"].AsBsonDocument["scopeKey"].AsString);
        Assert.Equal(NowUtc, update["$set"].AsBsonDocument["updatedAt"].ToUniversalTime());
        Assert.Equal(1, update["$inc"].AsBsonDocument["pendingMutationCount"].AsInt32);
        Assert.Equal(
            leaseExpiresAtUtc,
            update["$max"].AsBsonDocument["mutationLeaseExpiresAtUtc"].ToUniversalTime());
        Assert.False(update["$inc"].AsBsonDocument.Contains("revision"));
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

        await repository.BeginMutationAsync(
            scopeKey,
            CancellationToken.None);

        collection.VerifyAll();
        database.VerifyAll();
    }

    [Fact]
    public async Task CompleteMutationAsync_ShouldAtomicallyAdvanceRevisionBeforeRemovingLastLease()
    {
        RankingScopeKey scopeKey = RankingScopeKey.Parse("parks:global");
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
                FilterDefinition<RatingRankingSourceRevisionDocument> _,
                UpdateDefinition<RatingRankingSourceRevisionDocument> update,
                FindOneAndUpdateOptions<RatingRankingSourceRevisionDocument, RatingRankingSourceRevisionDocument> _,
                CancellationToken _) => capturedUpdate = update)
            .ReturnsAsync(new RatingRankingSourceRevisionDocument
            {
                Id = scopeKey.Value,
                ScopeKey = scopeKey.Value,
                Revision = 42,
                PendingMutationCount = 0,
                MutationLeaseExpiresAtUtc = NowUtc.AddMinutes(30),
                CreatedAt = NowUtc,
                UpdatedAt = NowUtc,
            });
        collection
            .Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<RatingRankingSourceRevisionDocument>>(),
                It.IsAny<UpdateDefinition<RatingRankingSourceRevisionDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        RatingRankingSourceRevisionRepository repository = CreateRepository(collection.Object);

        RatingRankingSourceRevision revision = await repository.CompleteMutationAsync(
            scopeKey,
            sourceChanged: true,
            CancellationToken.None);

        Assert.NotNull(capturedUpdate);
        BsonDocument updateDocument = Render(capturedUpdate);
        Assert.Equal(-1, updateDocument["$inc"].AsBsonDocument["pendingMutationCount"].AsInt32);
        Assert.Equal(1, updateDocument["$inc"].AsBsonDocument["revision"].AsInt64);
        Assert.Equal(42, revision.Revision);
        Assert.True(revision.IsRebuildable);
        Assert.Null(revision.MutationLeaseExpiresAtUtc);
        collection.VerifyAll();
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
