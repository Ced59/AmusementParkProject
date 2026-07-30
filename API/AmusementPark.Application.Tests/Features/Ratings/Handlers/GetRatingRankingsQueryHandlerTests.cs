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

public sealed class GetRatingRankingsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenParkSearchMatches_ShouldReturnFiveRankingsAroundMatch()
    {
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetVisibleRankingSourcesAsync(null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateParkSources());

        GetRatingRankingsQueryHandler handler = new GetRatingRankingsQueryHandler(ratingRepository.Object, new PagedQueryValidator());

        ApplicationResult<PagedResult<ParkRatingRankingResult>> result = await handler.HandleAsync(
            new GetRatingRankingsQuery(null, new PagedQuery(1, 20), "Park 08"));

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value!.Items.Count);
        Assert.Equal(3, result.Value.Items.First().Rank);
        Assert.Contains(result.Value.Items, static item => item.ParkName == "Park 08");
        Assert.Equal(12, result.Value.Items.Last().Rank);
        ratingRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenAttractionTypeIsSelected_ShouldRankMatchingItemsAcrossParks()
    {
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetVisibleParkItemRankingSourcesAsync(
                ParkItemCategory.Attraction,
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                CreateParkItemSource("flat-1", "Talocan", "park-1", "Phantasialand", ParkItemType.FlatRide, 4.1),
                CreateParkItemSource("coaster-1", "Taron", "park-1", "Phantasialand", ParkItemType.RollerCoaster, 4.8),
                CreateParkItemSource("flat-2", "Sledge Hammer", "park-2", "Bobbejaanland", ParkItemType.FlatRide, 4.5),
            });
        GetParkItemRatingRankingsQueryHandler handler = new GetParkItemRatingRankingsQueryHandler(
            ratingRepository.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<ParkItemRatingRankingResult>> result = await handler.HandleAsync(
            new GetParkItemRatingRankingsQuery(
                ParkItemCategory.Attraction,
                new PagedQuery(1, 20),
                null,
                ParkItemType.FlatRide));

        Assert.True(result.IsSuccess);
        Assert.Collection(
            result.Value!.Items,
            first =>
            {
                Assert.Equal(1, first.Rank);
                Assert.Equal("Sledge Hammer", first.TargetName);
                Assert.Equal("Bobbejaanland", first.ParkName);
            },
            second =>
            {
                Assert.Equal(2, second.Rank);
                Assert.Equal("Talocan", second.TargetName);
                Assert.Equal("Phantasialand", second.ParkName);
            });
        ratingRepository.VerifyAll();
    }

    private static IReadOnlyCollection<RatingRankingItemResult> CreateParkSources()
    {
        List<RatingRankingItemResult> sources = new List<RatingRankingItemResult>();
        for (int index = 1; index <= 12; index += 1)
        {
            double score = 5d - (index * 0.1d);
            sources.Add(new RatingRankingItemResult(
                RatingTargetType.Park,
                $"park-{index:00}",
                $"Park {index:00}",
                $"park-{index:00}",
                $"Park {index:00}",
                null,
                null,
                10,
                score * 10,
                score,
                score));
        }

        return sources;
    }

    private static RatingRankingItemResult CreateParkItemSource(
        string targetId,
        string targetName,
        string parkId,
        string parkName,
        ParkItemType parkItemType,
        double bayesianScore)
    {
        return new RatingRankingItemResult(
            RatingTargetType.ParkItem,
            targetId,
            targetName,
            parkId,
            parkName,
            ParkItemCategory.Attraction,
            parkItemType,
            10,
            45,
            4.5,
            bayesianScore);
    }
}
