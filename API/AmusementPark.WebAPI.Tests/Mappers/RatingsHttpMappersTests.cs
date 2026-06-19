using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.WebAPI.Contracts.Ratings;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class RatingsHttpMappersTests
{
    [Fact]
    public void ToHttp_WhenSummaryIsMapped_ShouldExposeRatingNumbers()
    {
        RatingSummaryResult result = new RatingSummaryResult(
            RatingTargetType.Park,
            "park-1",
            12,
            4.35d,
            3.88d);

        RatingSummaryDto dto = result.ToHttp();

        Assert.Equal("Park", dto.TargetType);
        Assert.Equal("park-1", dto.TargetId);
        Assert.Equal(12, dto.RatingCount);
        Assert.Equal(4.35d, dto.AverageRating);
        Assert.Equal(3.88d, dto.BayesianScore);
    }

    [Fact]
    public void ToHttp_WhenParkRankingIsMapped_ShouldExposeTree()
    {
        ParkRatingRankingResult result = new ParkRatingRankingResult(
            1,
            "park-1",
            "Demo Park",
            12,
            4.2d,
            4,
            4.5d,
            8,
            4.1d,
            new[]
            {
                new ParkRatingRankingCategoryResult(
                    ParkItemCategory.Attraction,
                    8,
                    4.1d,
                    3.8d,
                    new[]
                    {
                        new ParkRatingRankingItemResult(
                            "item-1",
                            "Demo Attraction",
                            ParkItemCategory.Attraction,
                            ParkItemType.RollerCoaster,
                            8,
                            4.75d,
                            4.1d)
                    })
            });

        ParkRatingRankingDto dto = result.ToHttp();

        Assert.Equal(1, dto.Rank);
        Assert.Equal("park-1", dto.ParkId);
        Assert.Equal("Demo Park", dto.ParkName);
        Assert.Equal(12, dto.RatingCount);
        Assert.Equal(4.2d, dto.Score);
        Assert.Single(dto.Categories);
        ParkRatingRankingCategoryDto category = dto.Categories.Single();
        Assert.Equal("Attraction", category.ParkItemCategory);
        ParkRatingRankingItemDto item = category.Items.Single();
        Assert.Equal("item-1", item.TargetId);
        Assert.Equal("Demo Attraction", item.TargetName);
        Assert.Equal("RollerCoaster", item.ParkItemType);
        Assert.Equal(8, item.RatingCount);
    }

    [Fact]
    public void ToRatingTargetType_WhenValueIsCaseInsensitive_ShouldParseValue()
    {
        RatingTargetType targetType = "parkitem".ToRatingTargetType();

        Assert.Equal(RatingTargetType.ParkItem, targetType);
    }

    [Fact]
    public void ToParkItemCategoryFilter_WhenValueIsInvalid_ShouldReturnNull()
    {
        ParkItemCategory? category = "bad-category".ToParkItemCategoryFilter();

        Assert.Null(category);
    }
}
