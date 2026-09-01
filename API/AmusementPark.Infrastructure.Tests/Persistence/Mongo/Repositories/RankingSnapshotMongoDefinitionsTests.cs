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
    public void BuildFailedHeaderRestartFilter_ShouldClaimOnlyTheFailedAttempt()
    {
        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildFailedHeaderRestartFilter(
            RankingSnapshotId.Parse("snapshot-failed"),
            expectedBuildAttempt: 3));

        Assert.Equal("snapshot-failed", rendered["_id"].AsString);
        Assert.Equal(nameof(RankingSnapshotStatus.Failed), rendered["status"].AsString);
        Assert.Equal(3, rendered["buildAttempt"].AsInt32);
    }

    [Fact]
    public void BuildFailedHeaderRestartFilter_ShouldFenceALegacyFirstAttempt()
    {
        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildFailedHeaderRestartFilter(
            RankingSnapshotId.Parse("snapshot-legacy"),
            expectedBuildAttempt: 1));

        Assert.Equal("snapshot-legacy", rendered["_id"].AsString);
        Assert.Equal(nameof(RankingSnapshotStatus.Failed), rendered["status"].AsString);
        Assert.Contains(rendered["$or"].AsBsonArray, static item =>
            item["buildAttempt"].IsBsonDocument &&
            item["buildAttempt"].AsBsonDocument.Contains("$exists") &&
            !item["buildAttempt"].AsBsonDocument["$exists"].AsBoolean);
    }

    [Fact]
    public void BuildHeaderAttemptFilter_ShouldFenceAStaleWorker()
    {
        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildHeaderAttemptFilter(
            RankingSnapshotId.Parse("snapshot-current"),
            expectedBuildAttempt: 3));

        Assert.Equal("snapshot-current", rendered["_id"].AsString);
        Assert.Equal(3, rendered["buildAttempt"].AsInt32);
    }

    [Fact]
    public void BuildStaleChunkAttemptFilter_ShouldFenceEarlierAttempts()
    {
        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildStaleChunkAttemptFilter(
            RankingSnapshotId.Parse("snapshot-failed"),
            chunkIndex: 2,
            currentBuildAttempt: 3));

        Assert.Equal("snapshot-failed", rendered["snapshotId"].AsString);
        Assert.Equal(2, rendered["chunkIndex"].AsInt32);
        Assert.Contains(rendered["$or"].AsBsonArray, static item =>
            item["buildAttempt"].IsBsonDocument &&
            item["buildAttempt"].AsBsonDocument.Contains("$lt") &&
            item["buildAttempt"].AsBsonDocument["$lt"].AsInt32 == 3);
    }

    [Fact]
    public void BuildChunkAttemptAtMostFilter_ShouldPreserveChunksFromANewerRestart()
    {
        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildChunkAttemptAtMostFilter(
            RankingSnapshotId.Parse("snapshot-failed"),
            maximumBuildAttempt: 3));

        Assert.Equal("snapshot-failed", rendered["snapshotId"].AsString);
        Assert.Contains(rendered["$or"].AsBsonArray, static item =>
            item["buildAttempt"].IsBsonDocument &&
            item["buildAttempt"].AsBsonDocument.Contains("$lte") &&
            item["buildAttempt"].AsBsonDocument["$lte"].AsInt32 == 3);
    }

    [Fact]
    public void BuildChunkAttemptFilter_ShouldHideChunksFromAStaleWorker()
    {
        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildChunkAttemptFilter(
            RankingSnapshotId.Parse("snapshot-current"),
            expectedBuildAttempt: 4));

        Assert.Equal("snapshot-current", rendered["snapshotId"].AsString);
        Assert.Equal(4, rendered["buildAttempt"].AsInt32);
    }

    [Fact]
    public void BuildChunkIdentityAttemptFilter_ShouldDeleteOnlyTheStaleInsertedChunk()
    {
        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildChunkIdentityAttemptFilter(
            RankingSnapshotId.Parse("snapshot-current"),
            chunkIndex: 2,
            expectedBuildAttempt: 4));

        Assert.Equal("snapshot-current", rendered["snapshotId"].AsString);
        Assert.Equal(2, rendered["chunkIndex"].AsInt32);
        Assert.Equal(4, rendered["buildAttempt"].AsInt32);
    }

    [Fact]
    public void BuildOrphanChunkCleanupPipeline_ShouldFindOldChunksWithoutAHeaderInBoundedLots()
    {
        IReadOnlyCollection<BsonDocument> stages =
            RankingSnapshotMongoDefinitions.BuildOrphanChunkCleanupPipeline(
                ScopeKey,
                "custom-ranking-headers",
                NowUtc,
                maximumResultCount: 100);

        Assert.Equal(6, stages.Count);
        BsonDocument match = stages.ElementAt(0)["$match"].AsBsonDocument;
        Assert.Equal(ScopeKey.Value, match["scopeKey"].AsString);
        Assert.Equal(NowUtc, match["updatedAt"].AsBsonDocument["$lte"].ToUniversalTime());
        BsonDocument lookup = stages.ElementAt(1)["$lookup"].AsBsonDocument;
        Assert.Equal("custom-ranking-headers", lookup["from"].AsString);
        Assert.Equal("snapshotId", lookup["localField"].AsString);
        Assert.Equal("_id", lookup["foreignField"].AsString);
        Assert.Equal(0, stages.ElementAt(2)["$match"].AsBsonDocument["_snapshotHeader"]
            .AsBsonDocument["$size"].AsInt32);
        Assert.Equal(100, stages.ElementAt(4)["$limit"].AsInt32);
        Assert.Equal(1, stages.ElementAt(5)["$project"].AsBsonDocument["_id"].AsInt32);
    }

    [Fact]
    public void BuildConfirmedOrphanChunkPruneFilter_ShouldRecheckScopeIdentityAndAge()
    {
        BsonDocument rendered = Render(
            RankingSnapshotMongoDefinitions.BuildConfirmedOrphanChunkPruneFilter(
                ScopeKey,
                new[] { "snapshot-1:0", "snapshot-1:1" },
                NowUtc));

        Assert.Equal(ScopeKey.Value, rendered["scopeKey"].AsString);
        Assert.Equal(
            new[] { "snapshot-1:0", "snapshot-1:1" },
            rendered["_id"].AsBsonDocument["$in"].AsBsonArray.Select(static item => item.AsString));
        Assert.Equal(NowUtc, rendered["updatedAt"].AsBsonDocument["$lte"].ToUniversalTime());
    }

    [Fact]
    public void BuildRetentionCandidateFilter_ShouldKeepCurrentAndRollbackSnapshots()
    {
        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildRetentionCandidateFilter(
            ScopeKey,
            new[]
            {
                RankingSnapshotId.Parse("snapshot-current"),
                RankingSnapshotId.Parse("snapshot-previous"),
            },
            highestPublishedSourceRevision: 42,
            activeMethodologyVersion: MethodologyVersion));

        Assert.Equal(ScopeKey.Value, rendered["scopeKey"].AsString);
        BsonArray terminalConditions = rendered["$or"].AsBsonArray;
        Assert.Equal(
            new[]
            {
                nameof(RankingSnapshotStatus.Superseded),
                nameof(RankingSnapshotStatus.Failed),
            },
            terminalConditions[0]["status"].AsBsonDocument["$in"].AsBsonArray
                .Select(static item => item.AsString));
        Assert.Equal(
            nameof(RankingSnapshotStatus.Validated),
            terminalConditions[2]["status"].AsString);
        Assert.Equal(
            nameof(RankingSnapshotStatus.Building),
            terminalConditions[1]["status"].AsString);
        BsonArray staleRevisionConditions = terminalConditions[2]["$or"].AsBsonArray;
        Assert.Equal(42, staleRevisionConditions[0]["sourceRevision"].AsBsonDocument["$lt"].AsInt64);
        Assert.Equal(42, staleRevisionConditions[1]["sourceRevision"].AsInt64);
        Assert.Equal(
            MethodologyVersion.Value,
            staleRevisionConditions[1]["methodologyVersion"].AsBsonDocument["$ne"].AsString);
        Assert.Equal(
            new[] { "snapshot-current", "snapshot-previous" },
            rendered["_id"].AsBsonDocument["$nin"].AsBsonArray.Select(static item => item.AsString));
    }

    [Fact]
    public void BuildRetentionCandidateFilter_WithoutAnActiveMethodology_ShouldPruneTheHighWaterRevision()
    {
        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildRetentionCandidateFilter(
            ScopeKey,
            Array.Empty<RankingSnapshotId>(),
            highestPublishedSourceRevision: 42,
            activeMethodologyVersion: null));

        BsonArray terminalConditions = rendered["$or"].AsBsonArray;
        Assert.Equal(
            42,
            terminalConditions[2]["sourceRevision"].AsBsonDocument["$lte"].AsInt64);
    }

    [Fact]
    public void BuildRetentionCandidateFilter_WithoutAHighWater_ShouldNotSelectInProgressBuilds()
    {
        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildRetentionCandidateFilter(
            ScopeKey,
            Array.Empty<RankingSnapshotId>(),
            highestPublishedSourceRevision: null,
            activeMethodologyVersion: MethodologyVersion));

        BsonArray terminalConditions = rendered["$or"].AsBsonArray;
        Assert.Single(terminalConditions);
        Assert.Equal(
            new[]
            {
                nameof(RankingSnapshotStatus.Superseded),
                nameof(RankingSnapshotStatus.Failed),
            },
            terminalConditions[0]["status"].AsBsonDocument["$in"].AsBsonArray
                .Select(static item => item.AsString));
    }

    [Fact]
    public void BuildOrphanedCurrentHeadersReconciliationFilter_ShouldProtectTheLivePointerPair()
    {
        RankingPublicationPointer pointer = new RankingPublicationPointer(
            ScopeKey,
            RankingSnapshotId.Parse("snapshot-current"),
            NowUtc.AddMinutes(-2),
            RankingSnapshotId.Parse("snapshot-previous"),
            NowUtc.AddMinutes(-1),
            MethodologyVersion,
            sourceRevision: 42,
            highestPublishedSourceRevision: 42,
            version: 7,
            NowUtc);

        BsonDocument rendered = Render(
            RankingSnapshotMongoDefinitions.BuildOrphanedCurrentHeadersReconciliationFilter(pointer));

        Assert.Equal(ScopeKey.Value, rendered["scopeKey"].AsString);
        Assert.Equal(nameof(RankingSnapshotStatus.Current), rendered["status"].AsString);
        Assert.Equal(
            new[] { "snapshot-current", "snapshot-previous" },
            rendered["_id"].AsBsonDocument["$nin"].AsBsonArray.Select(static item => item.AsString));
        Assert.Contains(rendered["$or"].AsBsonArray, static item =>
            item["reconciledPointerVersion"].IsBsonDocument &&
            item["reconciledPointerVersion"].AsBsonDocument.Contains("$lt") &&
            item["reconciledPointerVersion"].AsBsonDocument["$lt"].AsInt64 == 7);
    }

    [Fact]
    public void BuildSupersededHeaderPruneFilter_ShouldNotDeleteAnActiveSnapshot()
    {
        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildSupersededHeaderPruneFilter(
            RankingSnapshotId.Parse("snapshot-old")));

        Assert.Equal("snapshot-old", rendered["_id"].AsString);
        Assert.Equal(nameof(RankingSnapshotStatus.Superseded), rendered["status"].AsString);
    }

    [Fact]
    public void BuildStaleBuildingHeaderPruneFilter_ShouldFenceTheAttemptAndHighWater()
    {
        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildStaleBuildingHeaderPruneFilter(
            RankingSnapshotId.Parse("snapshot-building"),
            expectedBuildAttempt: 3,
            highestPublishedSourceRevision: 42,
            activeMethodologyVersion: MethodologyVersion));

        Assert.Equal("snapshot-building", rendered["_id"].AsString);
        Assert.Equal(3, rendered["buildAttempt"].AsInt32);
        Assert.Equal(nameof(RankingSnapshotStatus.Building), rendered["status"].AsString);
        BsonArray staleRevisionConditions = rendered["$or"].AsBsonArray;
        Assert.Equal(42, staleRevisionConditions[0]["sourceRevision"].AsBsonDocument["$lt"].AsInt64);
        Assert.Equal(42, staleRevisionConditions[1]["sourceRevision"].AsInt64);
        Assert.Equal(
            MethodologyVersion.Value,
            staleRevisionConditions[1]["methodologyVersion"].AsBsonDocument["$ne"].AsString);
    }

    [Fact]
    public void BuildStaleValidatedHeaderPruneFilter_ShouldFenceTheLiveHighWater()
    {
        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildStaleValidatedHeaderPruneFilter(
            RankingSnapshotId.Parse("snapshot-stale"),
            highestPublishedSourceRevision: 42,
            activeMethodologyVersion: MethodologyVersion));

        Assert.Equal("snapshot-stale", rendered["_id"].AsString);
        Assert.Equal(nameof(RankingSnapshotStatus.Validated), rendered["status"].AsString);
        BsonArray staleRevisionConditions = rendered["$or"].AsBsonArray;
        Assert.Equal(42, staleRevisionConditions[0]["sourceRevision"].AsBsonDocument["$lt"].AsInt64);
        Assert.Equal(42, staleRevisionConditions[1]["sourceRevision"].AsInt64);
        Assert.Equal(
            MethodologyVersion.Value,
            staleRevisionConditions[1]["methodologyVersion"].AsBsonDocument["$ne"].AsString);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    public void NormalizeBuildAttempt_ShouldSupportLegacyDocuments(
        int storedAttempt,
        int expectedAttempt)
    {
        Assert.Equal(expectedAttempt, RankingSnapshotMongoDefinitions.NormalizeBuildAttempt(storedAttempt));
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
            NowUtc,
            null,
            null,
            MethodologyVersion,
            42,
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
    public void IsStale_AfterRollback_ShouldRetainThePublishedRevisionHighWatermark()
    {
        RankingPublicationPointer rolledBack = new RankingPublicationPointer(
            ScopeKey,
            RankingSnapshotId.Parse("snapshot-restored"),
            NowUtc.AddMinutes(-2),
            RankingSnapshotId.Parse("snapshot-rolled-back"),
            NowUtc.AddMinutes(-1),
            MethodologyVersion,
            sourceRevision: 42,
            highestPublishedSourceRevision: 50,
            version: 4,
            NowUtc);

        Assert.True(RankingSnapshotMongoDefinitions.IsStale(
            rolledBack,
            CreateHeader(45, MethodologyVersion)));
        Assert.False(RankingSnapshotMongoDefinitions.IsStale(
            rolledBack,
            CreateHeader(51, MethodologyVersion)));
    }

    [Fact]
    public void BuildLivePointerFilter_ShouldFenceSnapshotAndPointerVersion()
    {
        RankingPublicationPointer pointer = new RankingPublicationPointer(
            ScopeKey,
            RankingSnapshotId.Parse("snapshot-current"),
            NowUtc,
            null,
            null,
            MethodologyVersion,
            sourceRevision: 42,
            highestPublishedSourceRevision: 42,
            version: 7,
            NowUtc);

        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildLivePointerFilter(pointer));

        Assert.Equal(ScopeKey.Value, rendered["scopeKey"].AsString);
        Assert.Equal("snapshot-current", rendered["currentSnapshotId"].AsString);
        Assert.Equal(7, rendered["version"].AsInt64);
    }

    [Fact]
    public void BuildHeaderReconciliationFilter_ShouldRejectANewerReconciliationVersion()
    {
        BsonDocument rendered = Render(RankingSnapshotMongoDefinitions.BuildHeaderReconciliationFilter(
            RankingSnapshotId.Parse("snapshot-current"),
            pointerVersion: 7));

        Assert.Equal("snapshot-current", rendered["_id"].AsString);
        BsonArray alternatives = rendered["$or"].AsBsonArray;
        Assert.Contains(alternatives, static item =>
            item["reconciledPointerVersion"].IsBsonDocument &&
            item["reconciledPointerVersion"].AsBsonDocument.Contains("$lte") &&
            item["reconciledPointerVersion"].AsBsonDocument["$lte"].AsInt64 == 7);
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
