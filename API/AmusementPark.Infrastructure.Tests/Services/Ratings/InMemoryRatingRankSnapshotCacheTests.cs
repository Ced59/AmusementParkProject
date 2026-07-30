using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Services.Ratings;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Ratings;

public sealed class InMemoryRatingRankSnapshotCacheTests
{
    [Fact]
    public async Task GetRankAsync_WhenSnapshotIsReused_ShouldBuildRankingOnlyOnceUntilInvalidated()
    {
        RatingAggregate aggregate = new RatingAggregate
        {
            TargetType = RatingTargetType.ParkItem,
            TargetId = "taron",
            ParkId = "park-1",
            ParkItemCategory = ParkItemCategory.Attraction,
            ParkItemType = ParkItemType.RollerCoaster,
            RatingCount = 12,
            RatingSum = 57,
            AverageRating = 4.75,
            BayesianScore = 4.42,
        };
        IReadOnlyCollection<RatingRankingItemResult> sources = new[]
        {
            CreateRankingSource("fly", "F.L.Y.", 4.6),
            CreateRankingSource("taron", "Taron", 4.42),
        };
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetVisibleParkItemRankingSourcesAsync(
                ParkItemCategory.Attraction,
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sources);

        using MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
        using InMemoryRatingRankSnapshotCache snapshotCache =
            new InMemoryRatingRankSnapshotCache(memoryCache);
        RatingRankProvider provider = new RatingRankProvider(ratingRepository.Object, snapshotCache);

        int? firstRank = await provider.GetRankAsync(aggregate, CancellationToken.None);
        int? cachedRank = await provider.GetRankAsync(aggregate, CancellationToken.None);
        provider.Invalidate();
        int? refreshedRank = await provider.GetRankAsync(aggregate, CancellationToken.None);

        Assert.Equal(2, firstRank);
        Assert.Equal(2, cachedRank);
        Assert.Equal(2, refreshedRank);
        ratingRepository.Verify(repository => repository.GetVisibleParkItemRankingSourcesAsync(
            ParkItemCategory.Attraction,
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        ratingRepository.VerifyNoOtherCalls();
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

    private static RatingRankingItemResult CreateRankingSource(
        string targetId,
        string targetName,
        double bayesianScore)
    {
        return new RatingRankingItemResult(
            RatingTargetType.ParkItem,
            targetId,
            targetName,
            "park-1",
            "Phantasialand",
            ParkItemCategory.Attraction,
            ParkItemType.RollerCoaster,
            10,
            45,
            4.5,
            bayesianScore);
    }
}
