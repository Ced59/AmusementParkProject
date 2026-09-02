using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Services.Ratings;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Ratings;

public sealed class InMemoryRatingRankSnapshotCacheTests
{
    [Fact]
    public async Task GetOrCreatePublishedAsync_WhenSnapshotIsReused_ShouldBuildOnlyOnceUntilInvalidated()
    {
        int factoryCalls = 0;
        using MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
        using InMemoryRatingRankSnapshotCache snapshotCache =
            new InMemoryRatingRankSnapshotCache(memoryCache);
        RankingScopeKey scopeKey = CanonicalRankingScopes.GlobalParks.Key;
        RatingMethodologyVersion methodologyVersion =
            RankingEligibilityPolicy.InitialMethodologyVersion;
        RatingPublishedRankingSnapshot publishedSnapshot = CreatePublishedSnapshot(
            scopeKey,
            RankingSnapshotId.Parse("snapshot-current"),
            methodologyVersion,
            sourceRevision: 4,
            pointerVersion: 1);
        Task<RatingPublishedRankingSnapshot?> CreateSnapshot(CancellationToken cancellationToken)
        {
            factoryCalls++;
            return Task.FromResult<RatingPublishedRankingSnapshot?>(publishedSnapshot);
        }

        RatingPublishedRankingSnapshot? first = await snapshotCache.GetOrCreatePublishedAsync(
            scopeKey,
            publishedSnapshot.SnapshotId,
            methodologyVersion,
            publishedSnapshot.SourceRevision,
            publishedSnapshot.PointerVersion,
            CreateSnapshot,
            CancellationToken.None);
        RatingPublishedRankingSnapshot? cached = await snapshotCache.GetOrCreatePublishedAsync(
            scopeKey,
            publishedSnapshot.SnapshotId,
            methodologyVersion,
            publishedSnapshot.SourceRevision,
            publishedSnapshot.PointerVersion,
            CreateSnapshot,
            CancellationToken.None);
        snapshotCache.Invalidate();
        RatingPublishedRankingSnapshot? refreshed = await snapshotCache.GetOrCreatePublishedAsync(
            scopeKey,
            publishedSnapshot.SnapshotId,
            methodologyVersion,
            publishedSnapshot.SourceRevision,
            publishedSnapshot.PointerVersion,
            CreateSnapshot,
            CancellationToken.None);

        Assert.Same(publishedSnapshot, first);
        Assert.Same(publishedSnapshot, cached);
        Assert.Same(publishedSnapshot, refreshed);
        Assert.Equal(2, factoryCalls);
    }

    [Fact]
    public async Task GetOrCreatePublishedAsync_WhenDifferentScopesRefresh_ShouldNotSerializeFactories()
    {
        using MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
        using InMemoryRatingRankSnapshotCache snapshotCache =
            new InMemoryRatingRankSnapshotCache(memoryCache);
        RatingMethodologyVersion methodologyVersion =
            RankingEligibilityPolicy.InitialMethodologyVersion;
        RatingPublishedRankingSnapshot parkSnapshot = CreatePublishedSnapshot(
            CanonicalRankingScopes.GlobalParks.Key,
            RankingSnapshotId.Parse("snapshot-parks"),
            methodologyVersion,
            sourceRevision: 4,
            pointerVersion: 1);
        RankingScopeDefinition itemScope = CanonicalRankingScopes.PublicItemCategories.First();
        RatingPublishedRankingSnapshot itemSnapshot = CreatePublishedSnapshot(
            itemScope.Key,
            RankingSnapshotId.Parse("snapshot-items"),
            itemScope.MethodologyVersion,
            sourceRevision: 5,
            pointerVersion: 1);
        TaskCompletionSource<bool> parkRefreshStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseParkRefresh = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Task<RatingPublishedRankingSnapshot?> parkRefresh = snapshotCache.GetOrCreatePublishedAsync(
            parkSnapshot.ScopeKey,
            parkSnapshot.SnapshotId,
            parkSnapshot.MethodologyVersion,
            parkSnapshot.SourceRevision,
            parkSnapshot.PointerVersion,
            async cancellationToken =>
            {
                parkRefreshStarted.SetResult(true);
                await releaseParkRefresh.Task.WaitAsync(cancellationToken);
                return parkSnapshot;
            },
            CancellationToken.None);

        await parkRefreshStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        try
        {
            RatingPublishedRankingSnapshot? itemResult =
                await snapshotCache.GetOrCreatePublishedAsync(
                    itemSnapshot.ScopeKey,
                    itemSnapshot.SnapshotId,
                    itemSnapshot.MethodologyVersion,
                    itemSnapshot.SourceRevision,
                    itemSnapshot.PointerVersion,
                    _ => Task.FromResult<RatingPublishedRankingSnapshot?>(itemSnapshot),
                    CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(1));

            Assert.Same(itemSnapshot, itemResult);
            Assert.False(parkRefresh.IsCompleted);
        }
        finally
        {
            releaseParkRefresh.TrySetResult(true);
        }

        RatingPublishedRankingSnapshot? parkResult = await parkRefresh;
        Assert.Same(parkSnapshot, parkResult);
    }

    [Fact]
    public async Task GetOrCreatePublishedAsync_WhenPointerIdentityChanges_ShouldNotReusePreviousPublication()
    {
        using MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
        using InMemoryRatingRankSnapshotCache snapshotCache =
            new InMemoryRatingRankSnapshotCache(memoryCache);
        RankingScopeKey scopeKey = CanonicalRankingScopes.GlobalParks.Key;
        RatingMethodologyVersion methodologyVersion =
            RankingEligibilityPolicy.InitialMethodologyVersion;
        int firstFactoryCalls = 0;
        int secondFactoryCalls = 0;
        RatingPublishedRankingSnapshot firstSnapshot = CreatePublishedSnapshot(
            scopeKey,
            RankingSnapshotId.Parse("snapshot-1"),
            methodologyVersion,
            sourceRevision: 4,
            pointerVersion: 1);
        RatingPublishedRankingSnapshot secondSnapshot = CreatePublishedSnapshot(
            scopeKey,
            RankingSnapshotId.Parse("snapshot-2"),
            methodologyVersion,
            sourceRevision: 5,
            pointerVersion: 2);

        RatingPublishedRankingSnapshot? first = await snapshotCache.GetOrCreatePublishedAsync(
            scopeKey,
            firstSnapshot.SnapshotId,
            methodologyVersion,
            firstSnapshot.SourceRevision,
            firstSnapshot.PointerVersion,
            _ =>
            {
                firstFactoryCalls++;
                return Task.FromResult<RatingPublishedRankingSnapshot?>(firstSnapshot);
            },
            CancellationToken.None);
        RatingPublishedRankingSnapshot? firstCached = await snapshotCache.GetOrCreatePublishedAsync(
            scopeKey,
            firstSnapshot.SnapshotId,
            methodologyVersion,
            firstSnapshot.SourceRevision,
            firstSnapshot.PointerVersion,
            _ =>
            {
                firstFactoryCalls++;
                return Task.FromResult<RatingPublishedRankingSnapshot?>(firstSnapshot);
            },
            CancellationToken.None);
        RatingPublishedRankingSnapshot? second = await snapshotCache.GetOrCreatePublishedAsync(
            scopeKey,
            secondSnapshot.SnapshotId,
            methodologyVersion,
            secondSnapshot.SourceRevision,
            secondSnapshot.PointerVersion,
            _ =>
            {
                secondFactoryCalls++;
                return Task.FromResult<RatingPublishedRankingSnapshot?>(secondSnapshot);
            },
            CancellationToken.None);

        Assert.Same(firstSnapshot, first);
        Assert.Same(firstSnapshot, firstCached);
        Assert.Same(secondSnapshot, second);
        Assert.Equal(1, firstFactoryCalls);
        Assert.Equal(1, secondFactoryCalls);
    }

    private static RatingPublishedRankingSnapshot CreatePublishedSnapshot(
        RankingScopeKey scopeKey,
        RankingSnapshotId snapshotId,
        RatingMethodologyVersion methodologyVersion,
        long sourceRevision,
        long pointerVersion)
    {
        return new RatingPublishedRankingSnapshot(
            scopeKey,
            snapshotId,
            methodologyVersion,
            sourceRevision,
            pointerVersion,
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            Array.Empty<RankingSnapshotEntry>());
    }
}
