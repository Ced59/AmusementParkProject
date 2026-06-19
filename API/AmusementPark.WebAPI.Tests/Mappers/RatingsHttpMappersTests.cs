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
    public void ToHttp_WhenRankingItemIsMapped_ShouldExposeTargetMetadata()
    {
        RatingRankingItemResult result = new RatingRankingItemResult(
            2,
            RatingTargetType.ParkItem,
            "item-1",
            "Demo Attraction",
            "park-1",
            "Demo Park",
            ParkItemCategory.Attraction,
            ParkItemType.RollerCoaster,
            8,
            4.75d,
            4.1d);

        RatingRankingItemDto dto = result.ToHttp();

        Assert.Equal(2, dto.Rank);
        Assert.Equal("ParkItem", dto.TargetType);
        Assert.Equal("item-1", dto.TargetId);
        Assert.Equal("Demo Attraction", dto.TargetName);
        Assert.Equal("park-1", dto.ParkId);
        Assert.Equal("Demo Park", dto.ParkName);
        Assert.Equal("Attraction", dto.ParkItemCategory);
        Assert.Equal("RollerCoaster", dto.ParkItemType);
        Assert.Equal(8, dto.RatingCount);
        Assert.Equal(4.75d, dto.AverageRating);
        Assert.Equal(4.1d, dto.BayesianScore);
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
