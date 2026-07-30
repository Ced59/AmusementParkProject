using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Handlers;

public sealed class GetRatingSummaryQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenAggregateDoesNotExist_ShouldReturnEmptySummaryWithPriorScore()
    {
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetAggregateAsync(RatingTargetType.Park, "park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RatingAggregate?)null);

        GetRatingSummaryQueryHandler handler = new GetRatingSummaryQueryHandler(ratingRepository.Object);

        ApplicationResult<RatingSummaryResult> result = await handler.HandleAsync(new GetRatingSummaryQuery(
            RatingTargetType.Park,
            " park-1 "));

        Assert.True(result.IsSuccess);
        Assert.Equal(RatingTargetType.Park, result.Value!.TargetType);
        Assert.Equal("park-1", result.Value.TargetId);
        Assert.Equal(0, result.Value.RatingCount);
        Assert.Equal(0d, result.Value.AverageRating);
        Assert.Equal(RatingScoreCalculator.PriorMean, result.Value.BayesianScore);
        ratingRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenParkItemIsRanked_ShouldReturnItsCategoryRank()
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
            CreateRankingSource("fly", "F.L.Y.", "park-1", "Phantasialand", ParkItemType.RollerCoaster, 4.6),
            CreateRankingSource("taron", "Taron", "park-1", "Phantasialand", ParkItemType.RollerCoaster, 4.42),
            CreateRankingSource("zadra", "Zadra", "park-2", "Energylandia", ParkItemType.RollerCoaster, 4.2),
        };
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetAggregateAsync(
                RatingTargetType.ParkItem,
                "taron",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregate);
        ratingRepository
            .Setup(repository => repository.GetVisibleParkItemRankingSourcesAsync(
                ParkItemCategory.Attraction,
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sources);

        GetRatingSummaryQueryHandler handler = new GetRatingSummaryQueryHandler(ratingRepository.Object);

        ApplicationResult<RatingSummaryResult> result = await handler.HandleAsync(
            new GetRatingSummaryQuery(RatingTargetType.ParkItem, "taron"));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Rank);
        ratingRepository.VerifyAll();
    }

    private static RatingRankingItemResult CreateRankingSource(
        string targetId,
        string targetName,
        string parkId,
        string parkName,
        ParkItemType parkItemType,
        double bayesianScore)
    {
        return new RatingRankingItemResult(
            RatingTargetType.ParkItem,
            targetId,
            targetName,
            parkId,
            parkName,
            ParkItemCategory.Attraction,
            parkItemType,
            10,
            45,
            4.5,
            bayesianScore);
    }
}
