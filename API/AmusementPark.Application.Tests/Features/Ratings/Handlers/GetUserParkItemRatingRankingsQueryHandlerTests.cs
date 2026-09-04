using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Handlers;

public sealed class GetUserParkItemRatingRankingsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ForPublicShare_ShouldRequestOnlyVisibleRankingSources()
    {
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetVisibleUserRankingSourcesAsync(
                "user-1",
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserRatingListItemResult>());
        GetUserParkItemRatingRankingsQueryHandler handler = new GetUserParkItemRatingRankingsQueryHandler(
            ratingRepository.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<UserParkItemRatingRankingResult>> result = await handler.HandleAsync(
            new GetUserParkItemRatingRankingsQuery(
                "user-1",
                ParkItemCategory.Attraction,
                new PagedQuery(1, 10),
                PublicTargetsOnly: true));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        ratingRepository.VerifyAll();
        ratingRepository.Verify(
            repository => repository.GetUserRankingSourcesAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenFlatRideIsSelected_ShouldRankOnlyPersonalFlatRideRatings()
    {
        IReadOnlyCollection<UserRatingListItemResult> sources = new[]
        {
            CreateRating("rating-1", "flat-1", "Talocan", "park-1", "Phantasialand", ParkItemType.FlatRide, 4d),
            CreateRating("rating-2", "coaster-1", "Taron", "park-1", "Phantasialand", ParkItemType.RollerCoaster, 5d),
            CreateRating("rating-3", "flat-2", "Sledge Hammer", "park-2", "Bobbejaanland", ParkItemType.FlatRide, 4.5d),
        };
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetUserRankingSourcesAsync(
                "user-1",
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sources);
        GetUserParkItemRatingRankingsQueryHandler handler = new GetUserParkItemRatingRankingsQueryHandler(
            ratingRepository.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<UserParkItemRatingRankingResult>> result = await handler.HandleAsync(
            new GetUserParkItemRatingRankingsQuery(
                " user-1 ",
                ParkItemCategory.Attraction,
                new PagedQuery(1, 10),
                null,
                ParkItemType.FlatRide));

        Assert.True(result.IsSuccess);
        Assert.Collection(
            result.Value!.Items,
            first =>
            {
                Assert.Equal(1, first.Rank);
                Assert.Equal("Sledge Hammer", first.Rating.TargetName);
                Assert.Equal("Bobbejaanland", first.Rating.ParkName);
            },
            second =>
            {
                Assert.Equal(2, second.Rank);
                Assert.Equal("Talocan", second.Rating.TargetName);
                Assert.Equal("Phantasialand", second.Rating.ParkName);
            });
        ratingRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenSearchHasSeveralPages_ShouldReturnRequestedPage()
    {
        IReadOnlyCollection<UserRatingListItemResult> sources = new[]
        {
            CreateRating("rating-1", "ride-1", "Ride Alpha", "park-1", "Demo Park", ParkItemType.FlatRide, 5d),
            CreateRating("rating-2", "ride-2", "Ride Beta", "park-1", "Demo Park", ParkItemType.FlatRide, 4.5d),
            CreateRating("rating-3", "ride-3", "Ride Gamma", "park-2", "Other Park", ParkItemType.FlatRide, 4d),
        };
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetUserRankingSourcesAsync(
                "user-1",
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sources);
        GetUserParkItemRatingRankingsQueryHandler handler = new GetUserParkItemRatingRankingsQueryHandler(
            ratingRepository.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<UserParkItemRatingRankingResult>> result = await handler.HandleAsync(
            new GetUserParkItemRatingRankingsQuery(
                "user-1",
                ParkItemCategory.Attraction,
                new PagedQuery(2, 1),
                " ride "));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Page);
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Equal(3, result.Value.TotalPages);
        UserParkItemRatingRankingResult ranking = Assert.Single(result.Value.Items);
        Assert.Equal(2, ranking.Rank);
        Assert.Equal("Ride Beta", ranking.Rating.TargetName);
        ratingRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WithTargetId_ReturnsTheExactOwnedTargetDespiteAmbiguousSearch()
    {
        List<UserRatingListItemResult> sources = Enumerable.Range(1, 11)
            .Select(index => CreateRating(
                $"rating-{index}",
                $"carousel-{index}",
                "Carousel",
                "park-1",
                "Demo Park",
                ParkItemType.FlatRide,
                5d - (index * 0.1d)))
            .ToList();
        sources.Add(CreateRating(
            "rating-12",
            "carousel-12",
            "Carousel",
            "park-1",
            "Demo Park",
            ParkItemType.FlatRide,
            3.8d) with
        {
            ParkItemCategory = ParkItemCategory.Restaurant,
        });
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetUserRankingSourcesAsync(
                "user-1",
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sources);
        GetUserParkItemRatingRankingsQueryHandler handler =
            new GetUserParkItemRatingRankingsQueryHandler(
                ratingRepository.Object,
                new PagedQueryValidator());

        ApplicationResult<PagedResult<UserParkItemRatingRankingResult>> result =
            await handler.HandleAsync(new GetUserParkItemRatingRankingsQuery(
                "user-1",
                ParkItemCategory.Attraction,
                new PagedQuery(1, 10),
                "Carousel",
                TargetId: "carousel-12"));

        Assert.True(result.IsSuccess);
        UserParkItemRatingRankingResult exactTarget = Assert.Single(result.Value!.Items);
        Assert.Equal("carousel-12", exactTarget.Rating.TargetId);
        ratingRepository.VerifyAll();
    }

    private static UserRatingListItemResult CreateRating(
        string id,
        string targetId,
        string targetName,
        string parkId,
        string parkName,
        ParkItemType parkItemType,
        double value)
    {
        RatingSummaryResult summary = new RatingSummaryResult(
            RatingTargetType.ParkItem,
            targetId,
            5,
            4d,
            3.8d);
        return new UserRatingListItemResult(
            id,
            RatingTargetType.ParkItem,
            targetId,
            targetName,
            parkId,
            parkName,
            ParkItemCategory.Attraction,
            parkItemType,
            value,
            DateTime.UtcNow,
            summary);
    }
}
