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

public sealed class DeleteUserRatingCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenRatingExists_ShouldDeleteItAndReturnRecalculatedSummary()
    {
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
            .Setup(repository => repository.DeleteUserRatingAndRecalculateAggregateAsync(
                "user-1",
                RatingTargetType.ParkItem,
                "item-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregate);
        Mock<IRatingRankProvider> ratingRankProvider = CreateRankProvider();
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Demo Park", Status = ParkStatus.Operating });
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .SetupSequence(repository => repository.GetByIdAsync("item-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkItem
            {
                Id = "item-1",
                ParkId = "park-1",
                Name = "Demo Ride",
                Category = ParkItemCategory.Attraction,
                Type = ParkItemType.RollerCoaster,
                AttractionDetails = new AttractionDetails { Status = ParkItemStatusNormalizer.Operating },
            })
            .ReturnsAsync(new ParkItem
            {
                Id = "item-1",
                ParkId = "park-1",
                Name = "Demo Restaurant",
                Category = ParkItemCategory.Restaurant,
            });
        Mock<IRatingRankingMutationGuard> rankingMutationGuard =
            new Mock<IRatingRankingMutationGuard>(MockBehavior.Strict);
        RatingRankingMutationPreparation initialPreparation = new RatingRankingMutationPreparation(
            Array.Empty<RatingRankingMutationLease>());
        RatingRankingMutationPreparation authoritativePreparation = new RatingRankingMutationPreparation(
            new[]
            {
                new RatingRankingMutationLease(
                    RankingScopeKey.Parse("park-items:category:restaurant"),
                    Guid.NewGuid().ToString("N")),
            });
        rankingMutationGuard
            .Setup(value => value.PrepareMutationAsync(
                RatingTargetType.ParkItem,
                ParkItemCategory.Attraction,
                ParkItemCategory.Show,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(initialPreparation);
        rankingMutationGuard
            .Setup(value => value.CompleteMutationAsync(
                initialPreparation,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        rankingMutationGuard
            .Setup(value => value.PrepareMutationAsync(
                RatingTargetType.ParkItem,
                ParkItemCategory.Restaurant,
                null,
                CancellationToken.None))
            .ReturnsAsync(authoritativePreparation);
        rankingMutationGuard
            .Setup(value => value.CompleteMutationAsync(
                authoritativePreparation,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        DeleteUserRatingCommandHandler handler = new DeleteUserRatingCommandHandler(
            ratingRepository.Object,
            ratingRankProvider.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            rankingMutationGuard.Object);

        ApplicationResult<RatingSummaryResult> result = await handler.HandleAsync(
            new DeleteUserRatingCommand(" user-1 ", RatingTargetType.ParkItem, " item-1 "));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.RatingCount);
        Assert.Equal(4d, result.Value.AverageRating);
        Assert.Equal(3.5d, result.Value.BayesianScore);
        ratingRepository.VerifyAll();
        ratingRankProvider.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        rankingMutationGuard.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenRatingIsAlreadyAbsent_ShouldReturnCurrentSummary()
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
            .Setup(repository => repository.DeleteUserRatingAndRecalculateAggregateAsync(
                "user-1",
                RatingTargetType.Park,
                "park-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregate);
        Mock<IRatingRankProvider> ratingRankProvider = CreateRankProvider();
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Demo Park", Status = ParkStatus.Operating });
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IRatingRankingMutationGuard> rankingMutationGuard = CreateMutationGuard(
            RatingTargetType.Park,
            null,
            null);
        DeleteUserRatingCommandHandler handler = new DeleteUserRatingCommandHandler(
            ratingRepository.Object,
            ratingRankProvider.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            rankingMutationGuard.Object);

        ApplicationResult<RatingSummaryResult> result = await handler.HandleAsync(
            new DeleteUserRatingCommand("user-1", RatingTargetType.Park, "park-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.RatingCount);
        ratingRepository.VerifyAll();
        ratingRankProvider.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
        rankingMutationGuard.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenLastRatingIsDeleted_ShouldReturnAnEmptySummary()
    {
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.DeleteUserRatingAndRecalculateAggregateAsync(
                "user-1",
                RatingTargetType.Park,
                "park-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RatingAggregate?)null);
        Mock<IRatingRankProvider> ratingRankProvider = CreateRankProvider();
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Demo Park", Status = ParkStatus.Operating });
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IRatingRankingMutationGuard> rankingMutationGuard = CreateMutationGuard(
            RatingTargetType.Park,
            null,
            null);
        DeleteUserRatingCommandHandler handler = new DeleteUserRatingCommandHandler(
            ratingRepository.Object,
            ratingRankProvider.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            rankingMutationGuard.Object);

        ApplicationResult<RatingSummaryResult> result = await handler.HandleAsync(
            new DeleteUserRatingCommand("user-1", RatingTargetType.Park, "park-1"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(0, result.Value.RatingCount);
        Assert.Equal(0d, result.Value.AverageRating);
        Assert.Equal(RatingScoreCalculator.PriorMean, result.Value.BayesianScore);
        ratingRepository.VerifyAll();
        ratingRankProvider.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
        rankingMutationGuard.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenSourceRevisionCannotBePrepared_ShouldNotDeleteTheRating()
    {
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        Mock<IRatingRankProvider> ratingRankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Demo Park", Status = ParkStatus.Operating });
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IRatingRankingMutationGuard> rankingMutationGuard = new Mock<IRatingRankingMutationGuard>(MockBehavior.Strict);
        rankingMutationGuard
            .Setup(guard => guard.PrepareMutationAsync(
                RatingTargetType.Park,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Mongo unavailable"));
        DeleteUserRatingCommandHandler handler = new DeleteUserRatingCommandHandler(
            ratingRepository.Object,
            ratingRankProvider.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            rankingMutationGuard.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new DeleteUserRatingCommand("user-1", RatingTargetType.Park, "park-1")));

        parkRepository.VerifyAll();
        rankingMutationGuard.VerifyAll();
        ratingRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyNoOtherCalls();
        ratingRankProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenRetainedTargetIsUnavailable_ShouldReturnExcludedSummary()
    {
        RatingAggregate aggregate = new RatingAggregate
        {
            TargetType = RatingTargetType.Park,
            TargetId = "park-1",
            ParkId = "park-1",
            RatingCount = 2,
            UniqueContributorCount = 2,
            RatingSum = 9,
            AverageRating = 4.5,
            BayesianScore = 3.7,
            MutationVersion = 2,
            CalculatedVersion = 2,
            SourceIntegrityIsValid = true,
        };
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.DeleteUserRatingAndRecalculateAggregateAsync(
                "user-1",
                RatingTargetType.Park,
                "park-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregate);
        Mock<IRatingRankProvider> ratingRankProvider = CreateRankProvider();
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Future Park", Status = ParkStatus.Planned });
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IRatingRankingMutationGuard> rankingMutationGuard = CreateMutationGuard(
            RatingTargetType.Park,
            null,
            null);
        DeleteUserRatingCommandHandler handler = new DeleteUserRatingCommandHandler(
            ratingRepository.Object,
            ratingRankProvider.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            rankingMutationGuard.Object);

        ApplicationResult<RatingSummaryResult> result = await handler.HandleAsync(
            new DeleteUserRatingCommand("user-1", RatingTargetType.Park, "park-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(RankingEvidenceLevel.Excluded, result.Value?.Evidence?.Level);
        Assert.Equal(
            RankingIneligibilityReason.TargetUnavailable,
            result.Value?.Evidence?.IneligibilityReason);
        ratingRepository.VerifyAll();
        ratingRankProvider.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
        rankingMutationGuard.VerifyAll();
    }

    private static Mock<IRatingRankProvider> CreateRankProvider()
    {
        Mock<IRatingRankProvider> ratingRankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        ratingRankProvider
            .Setup(provider => provider.Invalidate());
        return ratingRankProvider;
    }

    private static Mock<IRatingRankingMutationGuard> CreateMutationGuard(
        RatingTargetType targetType,
        ParkItemCategory? currentParkItemCategory,
        ParkItemCategory? previousParkItemCategory)
    {
        Mock<IRatingRankingMutationGuard> guard =
            new Mock<IRatingRankingMutationGuard>(MockBehavior.Strict);
        RatingRankingMutationPreparation preparation = new RatingRankingMutationPreparation(
            Array.Empty<RatingRankingMutationLease>());
        guard
            .Setup(value => value.PrepareMutationAsync(
                targetType,
                currentParkItemCategory,
                previousParkItemCategory,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(preparation);
        guard
            .Setup(value => value.CompleteMutationAsync(
                preparation,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        return guard;
    }
}
