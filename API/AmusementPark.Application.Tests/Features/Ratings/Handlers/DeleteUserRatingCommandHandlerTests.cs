using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Handlers;

public sealed class DeleteUserRatingCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenRatingExists_ShouldDeleteItAndReturnRecalculatedSummary()
    {
        UserRating deletedRating = new UserRating
        {
            Id = "rating-1",
            UserId = "user-1",
            TargetType = RatingTargetType.ParkItem,
            TargetId = "item-1",
            ParkId = "park-1",
            ParkItemCategory = ParkItemCategory.Attraction,
            ParkItemType = ParkItemType.RollerCoaster,
            Value = 4.5d,
        };
        RatingAggregate aggregate = new RatingAggregate
        {
            TargetType = RatingTargetType.ParkItem,
            TargetId = "item-1",
            ParkId = "park-1",
            ParkItemCategory = ParkItemCategory.Attraction,
            ParkItemType = ParkItemType.RollerCoaster,
            RatingCount = 2,
            AverageRating = 4d,
            BayesianScore = 3.5d,
        };
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.DeleteUserRatingAsync(
                "user-1",
                RatingTargetType.ParkItem,
                "item-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedRating);
        ratingRepository
            .Setup(repository => repository.RecalculateAggregateAsync(
                It.Is<RatingAggregateTarget>(target =>
                    target.TargetType == RatingTargetType.ParkItem &&
                    target.TargetId == "item-1" &&
                    target.ParkId == "park-1" &&
                    target.ParkItemCategory == ParkItemCategory.Attraction &&
                    target.ParkItemType == ParkItemType.RollerCoaster),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregate);
        DeleteUserRatingCommandHandler handler = new DeleteUserRatingCommandHandler(ratingRepository.Object);

        ApplicationResult<RatingSummaryResult> result = await handler.HandleAsync(
            new DeleteUserRatingCommand(" user-1 ", RatingTargetType.ParkItem, " item-1 "));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.RatingCount);
        Assert.Equal(4d, result.Value.AverageRating);
        Assert.Equal(3.5d, result.Value.BayesianScore);
        ratingRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenRatingIsAlreadyAbsent_ShouldReturnCurrentSummaryWithoutRecalculation()
    {
        RatingAggregate aggregate = new RatingAggregate
        {
            TargetType = RatingTargetType.Park,
            TargetId = "park-1",
            ParkId = "park-1",
            RatingCount = 3,
            AverageRating = 4.5d,
            BayesianScore = 3.8d,
        };
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.DeleteUserRatingAsync(
                "user-1",
                RatingTargetType.Park,
                "park-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRating?)null);
        ratingRepository
            .Setup(repository => repository.GetAggregateAsync(
                RatingTargetType.Park,
                "park-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregate);
        DeleteUserRatingCommandHandler handler = new DeleteUserRatingCommandHandler(ratingRepository.Object);

        ApplicationResult<RatingSummaryResult> result = await handler.HandleAsync(
            new DeleteUserRatingCommand("user-1", RatingTargetType.Park, "park-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.RatingCount);
        ratingRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenLastRatingIsDeleted_ShouldReturnAnEmptySummary()
    {
        UserRating deletedRating = new UserRating
        {
            Id = "rating-1",
            UserId = "user-1",
            TargetType = RatingTargetType.Park,
            TargetId = "park-1",
            ParkId = "park-1",
            Value = 4d,
        };
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.DeleteUserRatingAsync(
                "user-1",
                RatingTargetType.Park,
                "park-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedRating);
        ratingRepository
            .Setup(repository => repository.RecalculateAggregateAsync(
                It.Is<RatingAggregateTarget>(target =>
                    target.TargetType == RatingTargetType.Park
                    && target.TargetId == "park-1"
                    && target.ParkId == "park-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RatingAggregate?)null);
        DeleteUserRatingCommandHandler handler = new DeleteUserRatingCommandHandler(ratingRepository.Object);

        ApplicationResult<RatingSummaryResult> result = await handler.HandleAsync(
            new DeleteUserRatingCommand("user-1", RatingTargetType.Park, "park-1"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(0, result.Value.RatingCount);
        Assert.Equal(0d, result.Value.AverageRating);
        Assert.Equal(RatingScoreCalculator.PriorMean, result.Value.BayesianScore);
        ratingRepository.VerifyAll();
    }
}
