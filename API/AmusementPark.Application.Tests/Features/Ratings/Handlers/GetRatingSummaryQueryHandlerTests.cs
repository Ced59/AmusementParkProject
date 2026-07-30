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
        Mock<IRatingRankProvider> ratingRankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);

        GetRatingSummaryQueryHandler handler = new GetRatingSummaryQueryHandler(
            ratingRepository.Object,
            ratingRankProvider.Object);

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
        ratingRankProvider.VerifyNoOtherCalls();
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
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetAggregateAsync(
                RatingTargetType.ParkItem,
                "taron",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregate);
        Mock<IRatingRankProvider> ratingRankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        ratingRankProvider
            .Setup(provider => provider.GetRankAsync(aggregate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        GetRatingSummaryQueryHandler handler = new GetRatingSummaryQueryHandler(
            ratingRepository.Object,
            ratingRankProvider.Object);

        ApplicationResult<RatingSummaryResult> result = await handler.HandleAsync(
            new GetRatingSummaryQuery(RatingTargetType.ParkItem, "taron"));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Rank);
        ratingRepository.VerifyAll();
        ratingRankProvider.VerifyAll();
    }
}
