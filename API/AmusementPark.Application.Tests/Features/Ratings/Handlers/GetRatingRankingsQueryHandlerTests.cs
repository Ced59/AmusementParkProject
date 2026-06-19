using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Validation;
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
}
