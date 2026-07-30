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

public sealed class GetParkItemRatingRankingsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSearchHasSeveralPages_ShouldReturnRequestedPage()
    {
        IReadOnlyCollection<RatingRankingItemResult> sources = new[]
        {
            CreateRankingSource("ride-1", "Ride Alpha", 4.8),
            CreateRankingSource("ride-2", "Ride Beta", 4.6),
            CreateRankingSource("ride-3", "Ride Gamma", 4.4),
        };
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetVisibleParkItemRankingSourcesAsync(
                ParkItemCategory.Attraction,
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sources);
        GetParkItemRatingRankingsQueryHandler handler = new GetParkItemRatingRankingsQueryHandler(
            ratingRepository.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<ParkItemRatingRankingResult>> result = await handler.HandleAsync(
            new GetParkItemRatingRankingsQuery(
                ParkItemCategory.Attraction,
                new PagedQuery(2, 1),
                " ride "));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Page);
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Equal(3, result.Value.TotalPages);
        ParkItemRatingRankingResult ranking = Assert.Single(result.Value.Items);
        Assert.Equal(2, ranking.Rank);
        Assert.Equal("Ride Beta", ranking.TargetName);
        ratingRepository.VerifyAll();
    }

    private static RatingRankingItemResult CreateRankingSource(
        string targetId,
        string targetName,
        double bayesianScore)
    {
        return new RatingRankingItemResult(
            RatingTargetType.ParkItem,
            targetId,
            targetName,
            "park-1",
            "Demo Park",
            ParkItemCategory.Attraction,
            ParkItemType.RollerCoaster,
            10,
            45,
            4.5,
            bayesianScore);
    }
}
