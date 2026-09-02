using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Services.Ratings;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Ratings;

public sealed class InMemoryRatingRankSnapshotCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_WhenSnapshotIsReused_ShouldBuildRankingOnlyOnceUntilInvalidated()
    {
        int factoryCalls = 0;
        Task<IReadOnlyDictionary<string, int>> CreateRanks(CancellationToken cancellationToken)
        {
            factoryCalls++;
            IReadOnlyDictionary<string, int> ranks = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["fly"] = 1,
                ["taron"] = 2,
            };
            return Task.FromResult(ranks);
        }

        using MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
        using InMemoryRatingRankSnapshotCache snapshotCache =
            new InMemoryRatingRankSnapshotCache(memoryCache);

        IReadOnlyDictionary<string, int> firstRanks = await snapshotCache.GetOrCreateAsync(
            RatingTargetType.ParkItem,
            ParkItemCategory.Attraction,
            CreateRanks,
            CancellationToken.None);
        IReadOnlyDictionary<string, int> cachedRanks = await snapshotCache.GetOrCreateAsync(
            RatingTargetType.ParkItem,
            ParkItemCategory.Attraction,
            CreateRanks,
            CancellationToken.None);
        snapshotCache.Invalidate();
        IReadOnlyDictionary<string, int> refreshedRanks = await snapshotCache.GetOrCreateAsync(
            RatingTargetType.ParkItem,
            ParkItemCategory.Attraction,
            CreateRanks,
            CancellationToken.None);

        Assert.Equal(2, firstRanks["taron"]);
        Assert.Equal(2, cachedRanks["taron"]);
        Assert.Equal(2, refreshedRanks["taron"]);
        Assert.Equal(2, factoryCalls);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenDifferentSnapshotsRefresh_ShouldNotSerializeFactories()
    {
        using MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
        using InMemoryRatingRankSnapshotCache snapshotCache =
            new InMemoryRatingRankSnapshotCache(memoryCache);
        TaskCompletionSource<bool> parkRefreshStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseParkRefresh = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Task<IReadOnlyDictionary<string, int>> parkRefresh = snapshotCache.GetOrCreateAsync(
            RatingTargetType.Park,
            null,
            async cancellationToken =>
            {
                parkRefreshStarted.SetResult(true);
                await releaseParkRefresh.Task.WaitAsync(cancellationToken);
                return new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["park-1"] = 1,
                };
            },
            CancellationToken.None);

        await parkRefreshStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        try
        {
            IReadOnlyDictionary<string, int> itemRanks = await snapshotCache.GetOrCreateAsync(
                    RatingTargetType.ParkItem,
                    ParkItemCategory.Attraction,
                    static _ => Task.FromResult<IReadOnlyDictionary<string, int>>(
                        new Dictionary<string, int>(StringComparer.Ordinal)
                        {
                            ["item-1"] = 1,
                        }),
                    CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(1));

            Assert.Equal(1, itemRanks["item-1"]);
            Assert.False(parkRefresh.IsCompleted);
        }
        finally
        {
            releaseParkRefresh.TrySetResult(true);
        }

        IReadOnlyDictionary<string, int> parkRanks = await parkRefresh;
        Assert.Equal(1, parkRanks["park-1"]);
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
