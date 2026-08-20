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

public sealed class GetUserParkRatingRankingsQueryHandlerTests
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
        GetUserParkRatingRankingsQueryHandler handler = new GetUserParkRatingRankingsQueryHandler(
            ratingRepository.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<UserParkRatingRankingResult>> result = await handler.HandleAsync(
            new GetUserParkRatingRankingsQuery(
                "user-1",
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
    public async Task HandleAsync_ShouldRankParksAndKeepTheirRatedItemsGroupedByCategory()
    {
        IReadOnlyCollection<UserRatingListItemResult> sources = new[]
        {
            CreateRating("park-rating", RatingTargetType.Park, "park-1", "Phantasialand", "park-1", "Phantasialand", null, 5d),
            CreateRating("taron-rating", RatingTargetType.ParkItem, "taron", "Taron", "park-1", "Phantasialand", ParkItemCategory.Attraction, 4d),
            CreateRating("zadra-rating", RatingTargetType.ParkItem, "zadra", "Zadra", "park-2", "Energylandia", ParkItemCategory.Attraction, 3.5d),
        };
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetUserRankingSourcesAsync(
                "user-1",
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sources);
        GetUserParkRatingRankingsQueryHandler handler = new GetUserParkRatingRankingsQueryHandler(
            ratingRepository.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<UserParkRatingRankingResult>> result = await handler.HandleAsync(
            new GetUserParkRatingRankingsQuery("user-1", new PagedQuery(1, 10)));

        Assert.True(result.IsSuccess);
        UserParkRatingRankingResult first = Assert.Single(
            result.Value!.Items,
            static ranking => ranking.Rank == 1);
        Assert.Equal("Phantasialand", first.ParkName);
        Assert.Equal("park-rating", first.ParkRating!.Id);
        UserParkRatingRankingCategoryResult category = Assert.Single(first.Categories);
        Assert.Equal(ParkItemCategory.Attraction, category.ParkItemCategory);
        Assert.Equal("Taron", Assert.Single(category.Items).TargetName);
        ratingRepository.VerifyAll();
    }

    private static UserRatingListItemResult CreateRating(
        string id,
        RatingTargetType targetType,
        string targetId,
        string targetName,
        string parkId,
        string parkName,
        ParkItemCategory? category,
        double value)
    {
        RatingSummaryResult summary = new RatingSummaryResult(
            targetType,
            targetId,
            5,
            4d,
            3.8d);
        return new UserRatingListItemResult(
            id,
            targetType,
            targetId,
            targetName,
            parkId,
            parkName,
            category,
            category.HasValue ? ParkItemType.RollerCoaster : null,
            value,
            DateTime.UtcNow,
            summary);
    }
}
