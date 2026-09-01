using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Initialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Initialization;

public sealed class MongoDatabaseInitializerRankingSnapshotsTests
{
    [Fact]
    public void MongoSettings_ShouldUseTheVersionedRoadmapCollectionNames()
    {
        MongoDbSettings settings = new MongoDbSettings();

        Assert.Equal("rating-ranking-snapshot-headers", settings.RatingRankingSnapshotHeadersCollectionName);
        Assert.Equal("rating-ranking-snapshot-chunks", settings.RatingRankingSnapshotChunksCollectionName);
        Assert.Equal("rating-ranking-publication-pointers", settings.RatingRankingPublicationPointersCollectionName);
    }

    [Fact]
    public void BuildRankingSnapshotHeaderIndexes_ShouldProtectRevisionAndSupportLifecycleDiagnostics()
    {
        IReadOnlyCollection<CreateIndexModel<RankingSnapshotHeaderDocument>> indexes =
            MongoDatabaseInitializer.BuildRankingSnapshotHeaderIndexes();

        CreateIndexModel<RankingSnapshotHeaderDocument> revision = Assert.Single(
            indexes,
            static index => string.Equals(
                index.Options.Name,
                "idx_ranking_snapshot_header_source_unique",
                StringComparison.Ordinal));
        CreateIndexModel<RankingSnapshotHeaderDocument> lifecycle = Assert.Single(
            indexes,
            static index => string.Equals(
                index.Options.Name,
                "idx_ranking_snapshot_header_scope_status",
                StringComparison.Ordinal));

        Assert.True(revision.Options.Unique);
        Assert.Equal(
            new BsonDocument { { "scopeKey", 1 }, { "methodologyVersion", 1 }, { "sourceRevision", 1 } },
            Render(revision.Keys));
        Assert.Equal(
            new BsonDocument { { "scopeKey", 1 }, { "status", 1 }, { "generatedAtUtc", -1 } },
            Render(lifecycle.Keys));
        Assert.All(indexes, static index => Assert.Null(index.Options.ExpireAfter));
    }

    [Fact]
    public void BuildRankingSnapshotChunkIndexes_ShouldProtectChunkIdentityAndSupportBoundedPages()
    {
        IReadOnlyCollection<CreateIndexModel<RankingSnapshotChunkDocument>> indexes =
            MongoDatabaseInitializer.BuildRankingSnapshotChunkIndexes();

        CreateIndexModel<RankingSnapshotChunkDocument> identity = Assert.Single(
            indexes,
            static index => string.Equals(
                index.Options.Name,
                "idx_ranking_snapshot_chunk_index_unique",
                StringComparison.Ordinal));
        CreateIndexModel<RankingSnapshotChunkDocument> rankRange = Assert.Single(
            indexes,
            static index => string.Equals(
                index.Options.Name,
                "idx_ranking_snapshot_chunk_rank_range",
                StringComparison.Ordinal));

        Assert.True(identity.Options.Unique);
        Assert.Equal(new BsonDocument { { "snapshotId", 1 }, { "chunkIndex", 1 } }, Render(identity.Keys));
        Assert.Equal(
            new BsonDocument { { "snapshotId", 1 }, { "firstRank", 1 }, { "lastRank", 1 } },
            Render(rankRange.Keys));
        Assert.All(indexes, static index => Assert.Null(index.Options.ExpireAfter));
    }

    [Fact]
    public void BuildRankingPublicationPointerIndexes_ShouldAllowOnePointerPerScopeWithoutTtl()
    {
        CreateIndexModel<RankingPublicationPointerDocument> index = Assert.Single(
            MongoDatabaseInitializer.BuildRankingPublicationPointerIndexes());

        Assert.Equal("idx_ranking_publication_pointer_scope_unique", index.Options.Name);
        Assert.True(index.Options.Unique);
        Assert.Null(index.Options.ExpireAfter);
        Assert.Equal(new BsonDocument("scopeKey", 1), Render(index.Keys));
    }

    private static BsonDocument Render<TDocument>(IndexKeysDefinition<TDocument> keys)
    {
        IBsonSerializer<TDocument> serializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        RenderArgs<TDocument> arguments = new RenderArgs<TDocument>(serializer, BsonSerializer.SerializerRegistry);
        return keys.Render(arguments);
    }
}
