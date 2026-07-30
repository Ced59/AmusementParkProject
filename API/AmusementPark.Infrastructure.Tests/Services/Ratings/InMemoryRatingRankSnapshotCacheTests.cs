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
