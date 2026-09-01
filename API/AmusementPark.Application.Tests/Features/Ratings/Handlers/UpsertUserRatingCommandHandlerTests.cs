using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Models;
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
        Mock<IRatingRankProvider> ratingRankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        Mock<IRatingRankingMutationGuard> rankingMutationGuard = new Mock<IRatingRankingMutationGuard>(MockBehavior.Strict);
        UpsertUserRatingCommandHandler handler = new UpsertUserRatingCommandHandler(
            ratingRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            ratingRankProvider.Object,
            rankingMutationGuard.Object);

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
        ratingRankProvider.VerifyNoOtherCalls();
        rankingMutationGuard.VerifyNoOtherCalls();
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
            AttractionDetails = new AttractionDetails
            {
                Status = ParkItemStatusNormalizer.Operating,
            },
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
            .Setup(repository => repository.GetUserRatingAsync(
                "user-1",
                RatingTargetType.ParkItem,
                "item-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRating
            {
                UserId = "user-1",
                TargetType = RatingTargetType.ParkItem,
                TargetId = "item-1",
                ParkId = "park-1",
                ParkItemCategory = ParkItemCategory.Show,
            });
        ratingRepository
            .Setup(repository => repository.UpsertUserRatingAndRecalculateAggregateAsync(
                It.IsAny<UserRating>(),
                It.IsAny<RatingAggregateTarget>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRating rating, RatingAggregateTarget _, CancellationToken _) =>
            {
                rating.Id = "rating-1";
                rating.CreatedAtUtc = new DateTime(2026, 6, 19, 10, 0, 0, DateTimeKind.Utc);
                return new UserRatingMutationResult(rating, aggregate);
            });

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByIdAsync("item-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        Mock<IRatingRankProvider> ratingRankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        ratingRankProvider
            .Setup(provider => provider.Invalidate());
        Mock<IRatingRankingMutationGuard> rankingMutationGuard = new Mock<IRatingRankingMutationGuard>(MockBehavior.Strict);
        RatingRankingMutationPreparation preparation = new RatingRankingMutationPreparation(
            Array.Empty<RatingRankingSourceRevision>());
        rankingMutationGuard
            .Setup(guard => guard.PrepareMutationAsync(
                RatingTargetType.ParkItem,
                ParkItemCategory.Attraction,
                ParkItemCategory.Show,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(preparation);
        rankingMutationGuard
            .Setup(guard => guard.ScheduleRebuildsAsync(
                preparation,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        UpsertUserRatingCommandHandler handler = new UpsertUserRatingCommandHandler(
            ratingRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            ratingRankProvider.Object,
            rankingMutationGuard.Object);

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

        ratingRepository.Verify(repository => repository.UpsertUserRatingAndRecalculateAggregateAsync(
            It.Is<UserRating>(rating =>
                rating.UserId == "user-1" &&
                rating.TargetType == RatingTargetType.ParkItem &&
                rating.TargetId == "item-1" &&
                rating.ParkId == "park-1" &&
                rating.ParkItemCategory == ParkItemCategory.Attraction &&
                rating.ParkItemType == ParkItemType.RollerCoaster &&
                rating.Value == 4.5d),
            It.Is<RatingAggregateTarget>(target =>
                target.TargetType == RatingTargetType.ParkItem &&
                target.TargetId == "item-1" &&
                target.ParkId == "park-1" &&
                target.ParkItemCategory == ParkItemCategory.Attraction &&
                target.ParkItemType == ParkItemType.RollerCoaster),
            It.IsAny<CancellationToken>()), Times.Once);
        ratingRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        ratingRankProvider.VerifyAll();
        rankingMutationGuard.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenSourceRevisionCannotBePrepared_ShouldNotPersistTheRating()
    {
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Demo Park", Status = ParkStatus.Operating });
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IRatingRankProvider> ratingRankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        Mock<IRatingRankingMutationGuard> rankingMutationGuard = new Mock<IRatingRankingMutationGuard>(MockBehavior.Strict);
        rankingMutationGuard
            .Setup(guard => guard.PrepareMutationAsync(
                RatingTargetType.Park,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Mongo unavailable"));
        UpsertUserRatingCommandHandler handler = new UpsertUserRatingCommandHandler(
            ratingRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            ratingRankProvider.Object,
            rankingMutationGuard.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new UpsertUserRatingCommand("user-1", RatingTargetType.Park, "park-1", 4.5d)));

        parkRepository.VerifyAll();
        rankingMutationGuard.VerifyAll();
        ratingRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyNoOtherCalls();
        ratingRankProvider.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(ParkStatus.Planned)]
    [InlineData(ParkStatus.UnderConstruction)]
    [InlineData(ParkStatus.Cancelled)]
    public async Task HandleAsync_WhenParkStatusCannotRepresentAVisit_ShouldRejectRating(ParkStatus status)
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Future Park",
            Status = status,
        };
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IRatingRankProvider> ratingRankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        Mock<IRatingRankingMutationGuard> rankingMutationGuard = new Mock<IRatingRankingMutationGuard>(MockBehavior.Strict);
        UpsertUserRatingCommandHandler handler = new UpsertUserRatingCommandHandler(
            ratingRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            ratingRankProvider.Object,
            rankingMutationGuard.Object);

        ApplicationResult<UserRatingResult> result = await handler.HandleAsync(new UpsertUserRatingCommand(
            "user-1",
            RatingTargetType.Park,
            "park-1",
            4d));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "rating.target.unavailable");
        parkRepository.VerifyAll();
        ratingRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyNoOtherCalls();
        ratingRankProvider.VerifyNoOtherCalls();
        rankingMutationGuard.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(ParkItemStatusNormalizer.Planned)]
    [InlineData(ParkItemStatusNormalizer.UnderConstruction)]
    [InlineData(ParkItemStatusNormalizer.Unknown)]
    public async Task HandleAsync_WhenAttractionStatusCannotRepresentAVisit_ShouldRejectRating(string itemStatus)
    {
        ParkItem item = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Future Attraction",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
            AttractionDetails = new AttractionDetails
            {
                Status = itemStatus,
            },
        };
        Park park = new Park
        {
            Id = "park-1",
            Name = "Operating Park",
            Status = ParkStatus.Operating,
        };
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByIdAsync("item-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        Mock<IRatingRankProvider> ratingRankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        Mock<IRatingRankingMutationGuard> rankingMutationGuard = new Mock<IRatingRankingMutationGuard>(MockBehavior.Strict);
        UpsertUserRatingCommandHandler handler = new UpsertUserRatingCommandHandler(
            ratingRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            ratingRankProvider.Object,
            rankingMutationGuard.Object);

        ApplicationResult<UserRatingResult> result = await handler.HandleAsync(new UpsertUserRatingCommand(
            "user-1",
            RatingTargetType.ParkItem,
            "item-1",
            4d));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "rating.target.unavailable");
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        ratingRepository.VerifyNoOtherCalls();
        ratingRankProvider.VerifyNoOtherCalls();
        rankingMutationGuard.VerifyNoOtherCalls();
    }
}
