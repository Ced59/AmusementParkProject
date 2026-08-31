using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Handlers;

public sealed class GetUserRatingQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenRetainedParkIsUnavailable_ShouldReturnExcludedSummary()
    {
        UserRating rating = CreateRating(RatingTargetType.Park, "park-1", "park-1");
        RatingAggregate aggregate = CreateAggregate(RatingTargetType.Park, "park-1", "park-1");
        Mock<IRatingRepository> ratingRepository = CreateRatingRepository(rating, aggregate);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Future Park", Status = ParkStatus.Planned });
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        GetUserRatingQueryHandler handler = new GetUserRatingQueryHandler(
            ratingRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);

        ApplicationResult<UserRatingResult?> result = await handler.HandleAsync(
            new GetUserRatingQuery("user-1", RatingTargetType.Park, "park-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(RankingEvidenceLevel.Excluded, result.Value?.Summary.Evidence?.Level);
        Assert.Equal(
            RankingIneligibilityReason.TargetUnavailable,
            result.Value?.Summary.Evidence?.IneligibilityReason);
        ratingRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenRetainedParkItemIsUnavailable_ShouldReturnExcludedSummary()
    {
        UserRating rating = CreateRating(RatingTargetType.ParkItem, "item-1", "park-1");
        RatingAggregate aggregate = CreateAggregate(RatingTargetType.ParkItem, "item-1", "park-1");
        Mock<IRatingRepository> ratingRepository = CreateRatingRepository(rating, aggregate);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Demo Park", Status = ParkStatus.Operating });
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByIdAsync("item-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkItem
            {
                Id = "item-1",
                ParkId = "park-1",
                Name = "Future Ride",
                Category = ParkItemCategory.Attraction,
                Type = ParkItemType.RollerCoaster,
                AttractionDetails = new AttractionDetails { Status = ParkItemStatusNormalizer.Planned },
            });
        GetUserRatingQueryHandler handler = new GetUserRatingQueryHandler(
            ratingRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);

        ApplicationResult<UserRatingResult?> result = await handler.HandleAsync(
            new GetUserRatingQuery("user-1", RatingTargetType.ParkItem, "item-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(RankingEvidenceLevel.Excluded, result.Value?.Summary.Evidence?.Level);
        Assert.Equal(
            RankingIneligibilityReason.TargetUnavailable,
            result.Value?.Summary.Evidence?.IneligibilityReason);
        ratingRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenRetainedRatingHasNoAggregate_ShouldExposeIntegrityExclusion()
    {
        UserRating rating = CreateRating(RatingTargetType.Park, "park-1", "park-1");
        Mock<IRatingRepository> ratingRepository = CreateRatingRepository(rating, aggregate: null);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Demo Park", Status = ParkStatus.Operating });
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        GetUserRatingQueryHandler handler = new GetUserRatingQueryHandler(
            ratingRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);

        ApplicationResult<UserRatingResult?> result = await handler.HandleAsync(
            new GetUserRatingQuery("user-1", RatingTargetType.Park, "park-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(RankingEvidenceLevel.Excluded, result.Value?.Summary.Evidence?.Level);
        Assert.Equal(
            RankingIneligibilityReason.AggregateIntegrityFailure,
            result.Value?.Summary.Evidence?.IneligibilityReason);
        ratingRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
    }

    private static Mock<IRatingRepository> CreateRatingRepository(
        UserRating rating,
        RatingAggregate? aggregate)
    {
        Mock<IRatingRepository> repository = new Mock<IRatingRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetUserRatingAsync(
                "user-1",
                rating.TargetType,
                rating.TargetId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rating);
        repository
            .Setup(value => value.GetAggregateAsync(
                rating.TargetType,
                rating.TargetId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregate);
        return repository;
    }

    private static UserRating CreateRating(
        RatingTargetType targetType,
        string targetId,
        string parkId)
    {
        return new UserRating
        {
            Id = "rating-1",
            UserId = "user-1",
            TargetType = targetType,
            TargetId = targetId,
            ParkId = parkId,
            ParkItemCategory = targetType == RatingTargetType.ParkItem
                ? ParkItemCategory.Attraction
                : null,
            ParkItemType = targetType == RatingTargetType.ParkItem
                ? ParkItemType.RollerCoaster
                : null,
            Value = 4.5,
        };
    }

    private static RatingAggregate CreateAggregate(
        RatingTargetType targetType,
        string targetId,
        string parkId)
    {
        return new RatingAggregate
        {
            TargetType = targetType,
            TargetId = targetId,
            ParkId = parkId,
            ParkItemCategory = targetType == RatingTargetType.ParkItem
                ? ParkItemCategory.Attraction
                : null,
            ParkItemType = targetType == RatingTargetType.ParkItem
                ? ParkItemType.RollerCoaster
                : null,
            RatingCount = 10,
            UniqueContributorCount = 10,
            RatingSum = 45,
            AverageRating = 4.5,
            BayesianScore = 4,
            MutationVersion = 2,
            CalculatedVersion = 2,
        };
    }
}
