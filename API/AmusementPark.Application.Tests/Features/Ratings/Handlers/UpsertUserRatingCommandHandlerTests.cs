using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Handlers;

public sealed class UpsertUserRatingCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenRatingValueIsInvalid_ShouldRejectWithoutRepositoryCall()
    {
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        UpsertUserRatingCommandHandler handler = new UpsertUserRatingCommandHandler(
            ratingRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);

        ApplicationResult<UserRatingResult> result = await handler.HandleAsync(new UpsertUserRatingCommand(
            "user-1",
            RatingTargetType.Park,
            "park-1",
            4.25d));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "rating.value.invalid");
        ratingRepository.VerifyNoOtherCalls();
        parkRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenParkItemIsVisible_ShouldUpsertRatingAndReturnUpdatedSummary()
    {
        ParkItem item = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = " Demo Attraction ",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
            IsVisible = true,
        };
        Park park = new Park
        {
            Id = "park-1",
            Name = "Demo Park",
            IsVisible = true,
        };
        RatingAggregate aggregate = new RatingAggregate
        {
            TargetType = RatingTargetType.ParkItem,
            TargetId = "item-1",
            ParkId = "park-1",
            ParkItemCategory = ParkItemCategory.Attraction,
            ParkItemType = ParkItemType.RollerCoaster,
            RatingCount = 3,
            AverageRating = 4.5d,
            BayesianScore = 3.72d,
        };

        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.UpsertUserRatingAsync(It.IsAny<UserRating>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRating rating, CancellationToken _) =>
            {
                rating.Id = "rating-1";
                rating.CreatedAtUtc = new DateTime(2026, 6, 19, 10, 0, 0, DateTimeKind.Utc);
                return rating;
            });
        ratingRepository
            .Setup(repository => repository.RecalculateAggregateAsync(It.IsAny<RatingTargetMetadataResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregate);

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByIdAsync("item-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        UpsertUserRatingCommandHandler handler = new UpsertUserRatingCommandHandler(
            ratingRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);

        ApplicationResult<UserRatingResult> result = await handler.HandleAsync(new UpsertUserRatingCommand(
            " user-1 ",
            RatingTargetType.ParkItem,
            " item-1 ",
            4.5d));

        Assert.True(result.IsSuccess);
        Assert.Equal("rating-1", result.Value!.Id);
        Assert.Equal("user-1", result.Value.UserId);
        Assert.Equal(RatingTargetType.ParkItem, result.Value.TargetType);
        Assert.Equal("item-1", result.Value.TargetId);
        Assert.Equal("park-1", result.Value.ParkId);
        Assert.Equal(ParkItemCategory.Attraction, result.Value.ParkItemCategory);
        Assert.Equal(ParkItemType.RollerCoaster, result.Value.ParkItemType);
        Assert.Equal(4.5d, result.Value.Value);
        Assert.Equal(3, result.Value.Summary.RatingCount);
        Assert.Equal(4.5d, result.Value.Summary.AverageRating);
        Assert.Equal(3.72d, result.Value.Summary.BayesianScore);

        ratingRepository.Verify(repository => repository.UpsertUserRatingAsync(It.Is<UserRating>(rating =>
            rating.UserId == "user-1" &&
            rating.TargetType == RatingTargetType.ParkItem &&
            rating.TargetId == "item-1" &&
            rating.ParkId == "park-1" &&
            rating.ParkItemCategory == ParkItemCategory.Attraction &&
            rating.ParkItemType == ParkItemType.RollerCoaster &&
            rating.Value == 4.5d), It.IsAny<CancellationToken>()), Times.Once);
        ratingRepository.Verify(repository => repository.RecalculateAggregateAsync(It.Is<RatingTargetMetadataResult>(metadata =>
            metadata.TargetType == RatingTargetType.ParkItem &&
            metadata.TargetId == "item-1" &&
            metadata.TargetName == "Demo Attraction" &&
            metadata.ParkId == "park-1" &&
            metadata.ParkName == "Demo Park" &&
            metadata.ParkItemCategory == ParkItemCategory.Attraction &&
            metadata.ParkItemType == ParkItemType.RollerCoaster), It.IsAny<CancellationToken>()), Times.Once);
        ratingRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
    }
}
