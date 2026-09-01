using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Application.Features.Ratings.Services;
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
    public void BuildPageChunkFilter_ShouldOnlySelectTheRequiredChunkRangeFromTheSnapshot()
    {
        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildPageChunkFilter(
            RankingSnapshotId.Parse("snapshot-1"),
            0,
            1));

        Assert.Equal("snapshot-1", rendered["snapshotId"].AsString);
        Assert.Equal(0, rendered["chunkIndex"].AsBsonDocument["$gte"].AsInt32);
        Assert.Equal(1, rendered["chunkIndex"].AsBsonDocument["$lte"].AsInt32);
    }

    [Fact]
    public void IsStale_ShouldFenceSourceRevisionsWhileAllowingAnEqualRevisionForANewMethodology()
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
        Assert.True(RankingSnapshotMongoDefinitions.IsStale(
            pointer,
            CreateHeader(1, RatingMethodologyVersion.Parse("ratings-2027-01"))));
        Assert.False(RankingSnapshotMongoDefinitions.IsStale(
            pointer,
            CreateHeader(42, RatingMethodologyVersion.Parse("ratings-2027-01"))));
        Assert.False(RankingSnapshotMongoDefinitions.IsStale(
            pointer,
            CreateHeader(43, RatingMethodologyVersion.Parse("ratings-2027-01"))));
    }

    [Fact]
    public void IsPublishableForScope_ShouldRejectASnapshotBelowTheScopeMinimum()
    {
        RankingSnapshotHeader sparse = CreateHeader(42, MethodologyVersion, eligibleEntryCount: 2);
        RankingSnapshotHeader sufficient = CreateHeader(42, MethodologyVersion, eligibleEntryCount: 3);

        Assert.False(RankingSnapshotMongoDefinitions.IsPublishableForScope(
            sparse,
            CanonicalRankingScopes.GlobalParks));
        Assert.True(RankingSnapshotMongoDefinitions.IsPublishableForScope(
            sufficient,
            CanonicalRankingScopes.GlobalParks));
    }

    [Fact]
    public void IsPublishableForScope_ShouldRejectAnUnsupportedMethodology()
    {
        RankingSnapshotHeader previousMethodology = CreateHeader(
            41,
            RatingMethodologyVersion.Parse("ratings-2025-01"),
            eligibleEntryCount: 3);

        Assert.False(RankingSnapshotMongoDefinitions.IsPublishableForScope(
            previousMethodology,
            CanonicalRankingScopes.GlobalParks));
    }

    [Fact]
    public void ResolvePublishedAt_ShouldUseThePointerTimeAfterAnInterruptedPublication()
    {
        RankingSnapshotHeader validated = CreateHeader(42, MethodologyVersion, eligibleEntryCount: 3);
        DateTime pointerUpdatedAtUtc = NowUtc.AddMinutes(5);

        DateTime publishedAtUtc = RankingSnapshotMongoDefinitions.ResolvePublishedAt(
            validated,
            pointerUpdatedAtUtc);

        Assert.Equal(pointerUpdatedAtUtc, publishedAtUtc);
    }

    [Fact]
    public void ResolvePublishedAt_ShouldPreserveTheOriginalSnapshotPublicationTime()
    {
        RankingSnapshotHeader current = CreateHeader(
            42,
            MethodologyVersion,
            eligibleEntryCount: 3,
            RankingSnapshotStatus.Current);

        DateTime publishedAtUtc = RankingSnapshotMongoDefinitions.ResolvePublishedAt(
            current,
            NowUtc.AddHours(1));

        Assert.Equal(current.PublishedAtUtc, publishedAtUtc);
    }

    private static RankingSnapshotHeader CreateHeader(
        long sourceRevision,
        RatingMethodologyVersion methodologyVersion,
        int eligibleEntryCount = 0,
        RankingSnapshotStatus status = RankingSnapshotStatus.Validated)
    {
        DateTime? publishedAtUtc = status is RankingSnapshotStatus.Current or RankingSnapshotStatus.Superseded
            ? NowUtc.AddMinutes(1)
            : null;
        return new RankingSnapshotHeader(
            RankingSnapshotId.Parse($"snapshot-{sourceRevision}-{methodologyVersion.Value}"),
            ScopeKey,
            methodologyVersion,
            sourceRevision,
            status,
            eligibleEntryCount,
            eligibleEntryCount,
            500,
            eligibleEntryCount == 0 ? 0 : 1,
            RankingSnapshotChecksum.Parse(new string('a', 64)),
            NowUtc,
            NowUtc,
            publishedAtUtc);
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
