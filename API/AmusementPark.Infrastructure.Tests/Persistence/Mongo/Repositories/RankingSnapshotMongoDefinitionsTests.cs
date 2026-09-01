using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class RankingSnapshotMongoDefinitionsTests
{
    private static readonly RankingScopeKey ScopeKey = RankingScopeKey.Parse("parks:global");
    private static readonly RatingMethodologyVersion MethodologyVersion =
        RatingMethodologyVersion.Parse("ratings-2026-01");
    private static readonly DateTime NowUtc = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BuildHeaderNaturalKeyFilter_ShouldFenceOneScopeMethodologyAndRevision()
    {
        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildHeaderNaturalKeyFilter(
            ScopeKey,
            MethodologyVersion,
            42));

        Assert.Equal(ScopeKey.Value, rendered["scopeKey"].AsString);
        Assert.Equal(MethodologyVersion.Value, rendered["methodologyVersion"].AsString);
        Assert.Equal(42, rendered["sourceRevision"].AsInt64);
    }

    [Fact]
    public void BuildPointerVersionFilter_ShouldRequireScopeAndExpectedVersion()
    {
        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildPointerVersionFilter(ScopeKey, 7));

        Assert.Equal(ScopeKey.Value, rendered["scopeKey"].AsString);
        Assert.Equal(7, rendered["version"].AsInt64);
    }

    [Fact]
    public void BuildPageChunkFilter_ShouldOnlySelectOverlappingChunksFromTheSnapshot()
    {
        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildPageChunkFilter(
            RankingSnapshotId.Parse("snapshot-1"),
            451,
            550));

        Assert.Equal("snapshot-1", rendered["snapshotId"].AsString);
        Assert.Equal(550, rendered["firstRank"].AsBsonDocument["$lte"].AsInt32);
        Assert.Equal(451, rendered["lastRank"].AsBsonDocument["$gte"].AsInt32);
    }

    [Fact]
    public void IsStale_ShouldRejectAnEqualOrOlderRevisionForTheSameMethodology()
    {
        RankingPublicationPointer pointer = new RankingPublicationPointer(
            ScopeKey,
            RankingSnapshotId.Parse("snapshot-current"),
            null,
            MethodologyVersion,
            42,
            3,
            NowUtc);

        Assert.True(RankingSnapshotMongoDefinitions.IsStale(pointer, CreateHeader(42, MethodologyVersion)));
        Assert.True(RankingSnapshotMongoDefinitions.IsStale(pointer, CreateHeader(41, MethodologyVersion)));
        Assert.False(RankingSnapshotMongoDefinitions.IsStale(pointer, CreateHeader(43, MethodologyVersion)));
        Assert.False(RankingSnapshotMongoDefinitions.IsStale(
            pointer,
            CreateHeader(1, RatingMethodologyVersion.Parse("ratings-2027-01"))));
    }

    private static RankingSnapshotHeader CreateHeader(
        long sourceRevision,
        RatingMethodologyVersion methodologyVersion)
    {
        return new RankingSnapshotHeader(
            RankingSnapshotId.Parse($"snapshot-{sourceRevision}-{methodologyVersion.Value}"),
            ScopeKey,
            methodologyVersion,
            sourceRevision,
            RankingSnapshotStatus.Validated,
            0,
            0,
            500,
            0,
            RankingSnapshotChecksum.Parse(new string('a', 64)),
            NowUtc,
            NowUtc);
    }

    private static BsonDocument Render(FilterDefinition<RankingSnapshotHeaderDocument> filter)
    {
        IBsonSerializer<RankingSnapshotHeaderDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<RankingSnapshotHeaderDocument>();
        return filter.Render(new RenderArgs<RankingSnapshotHeaderDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument Render(FilterDefinition<RankingPublicationPointerDocument> filter)
    {
        IBsonSerializer<RankingPublicationPointerDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<RankingPublicationPointerDocument>();
        return filter.Render(new RenderArgs<RankingPublicationPointerDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument Render(FilterDefinition<RankingSnapshotChunkDocument> filter)
    {
        IBsonSerializer<RankingSnapshotChunkDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<RankingSnapshotChunkDocument>();
        return filter.Render(new RenderArgs<RankingSnapshotChunkDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }
}
