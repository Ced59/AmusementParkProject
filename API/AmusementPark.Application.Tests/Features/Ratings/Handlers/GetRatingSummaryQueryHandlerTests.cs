using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
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
}
