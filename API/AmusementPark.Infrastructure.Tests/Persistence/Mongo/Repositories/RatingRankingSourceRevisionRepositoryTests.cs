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
    public void BuildIncrementUpdate_ShouldAtomicallyCreateOrAdvanceOneCanonicalScope()
    {
        RankingScopeKey scopeKey = RankingScopeKey.Parse("parks:global");

        BsonDocument filter = Render(
            RatingRankingSourceRevisionMongoDefinitions.BuildScopeFilter(scopeKey));
        BsonDocument update = Render(
            RatingRankingSourceRevisionMongoDefinitions.BuildIncrementUpdate(scopeKey, NowUtc));

        Assert.Equal(scopeKey.Value, filter["_id"].AsString);
        Assert.Equal(scopeKey.Value, update["$setOnInsert"].AsBsonDocument["_id"].AsString);
        Assert.Equal(NowUtc, update["$setOnInsert"].AsBsonDocument["createdAt"].ToUniversalTime());
        Assert.Equal(scopeKey.Value, update["$set"].AsBsonDocument["scopeKey"].AsString);
        Assert.Equal(NowUtc, update["$set"].AsBsonDocument["updatedAt"].ToUniversalTime());
        Assert.Equal(1, update["$inc"].AsBsonDocument["revision"].AsInt64);
    }

    [Fact]
    public async Task IncrementAsync_ShouldReturnTheAtomicPostIncrementRevision()
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
                Revision = 42,
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

        RatingRankingSourceRevision revision = await repository.IncrementAsync(
            scopeKey,
            CancellationToken.None);

        Assert.Equal(scopeKey, revision.ScopeKey);
        Assert.Equal(42, revision.Revision);
        Assert.Equal(NowUtc, revision.UpdatedAtUtc);
        collection.VerifyAll();
        database.VerifyAll();
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
