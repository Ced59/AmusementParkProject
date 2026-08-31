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
    public void ToHttp_WhenSummaryHasNoEvidence_ShouldKeepLegacyCountWithoutInventingProof()
    {
        RatingSummaryResult result = new RatingSummaryResult(
            RatingTargetType.ParkItem,
            "item-legacy",
            4,
            4.25d,
            3.75d);

        RatingSummaryDto dto = result.ToHttp();

        Assert.Equal(4, dto.RatingCount);
        Assert.Equal(4, dto.RatingObservationCount);
        Assert.Null(dto.UniqueContributorCount);
        Assert.Null(dto.Evidence);
        Assert.Null(dto.MethodologyVersion);
    }

    [Fact]
    public void ToHttp_WhenSummaryIsMapped_ShouldExposeRatingNumbers()
    {
        RatingSummaryResult result = new RatingSummaryResult(
            RatingTargetType.Park,
            "park-1",
            12,
            4.35d,
            3.88d)
        {
            Rank = 2,
            Evidence = new RankingEvidenceResult(
                RankingEvidenceLevel.Eligible,
                true,
                12,
                12,
                null,
                null,
                null,
                null,
                RatingMethodologyVersion.Parse("ratings-2026-01"),
                null,
                30),
        };

        RatingSummaryDto dto = result.ToHttp();

        Assert.Equal("Park", dto.TargetType);
        Assert.Equal("park-1", dto.TargetId);
        Assert.Equal(12, dto.RatingCount);
        Assert.Equal(12, dto.RatingObservationCount);
        Assert.Equal(12, dto.UniqueContributorCount);
        Assert.Equal(4.35d, dto.AverageRating);
        Assert.Equal(3.88d, dto.BayesianScore);
        Assert.Equal(2, dto.Rank);
        Assert.Equal("Eligible", dto.Evidence?.Level);
        Assert.True(dto.Evidence?.IsEligibleForMainRanking);
        Assert.Null(dto.Evidence?.IneligibilityReason);
        Assert.Equal(30, dto.Evidence?.NextThreshold);
        Assert.Equal("ratings-2026-01", dto.MethodologyVersion);
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
            })
        {
            Evidence = new RankingEvidenceResult(
                RankingEvidenceLevel.Eligible,
                true,
                10,
                12,
                10,
                8,
                5,
                2,
                RatingMethodologyVersion.Parse("ratings-2026-01"),
                null,
                30),
        };

        ParkRatingRankingDto dto = result.ToHttp();

        Assert.Equal(1, dto.Rank);
        Assert.Equal("park-1", dto.ParkId);
        Assert.Equal("Demo Park", dto.ParkName);
        Assert.Equal(12, dto.RatingCount);
        Assert.Equal(12, dto.RatingObservationCount);
        Assert.Equal(10, dto.UniqueContributorCount);
        Assert.Equal(4.2d, dto.Score);
        Assert.Equal("Eligible", dto.Evidence?.Level);
        Assert.Equal(10, dto.Evidence?.DirectParkContributorCount);
        Assert.Equal(8, dto.Evidence?.ItemContributorCount);
        Assert.Equal(5, dto.Evidence?.EligibleItemCount);
        Assert.Equal(2, dto.Evidence?.EligibleCategoryCount);
        Assert.Equal("ratings-2026-01", dto.MethodologyVersion);
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

    [Fact]
    public void ToHttp_WhenParkItemRankingIsMapped_ShouldExposeParentPark()
    {
        ParkItemRatingRankingResult result = new ParkItemRatingRankingResult(
            3,
            "item-1",
            "Talocan",
            "park-1",
            "Phantasialand",
            ParkItemCategory.Attraction,
            ParkItemType.FlatRide,
            7,
            4.5d,
            4.2d)
        {
            Evidence = new RankingEvidenceResult(
                RankingEvidenceLevel.Provisional,
                false,
                7,
                7,
                null,
                null,
                null,
                null,
                RatingMethodologyVersion.Parse("ratings-2026-01"),
                RankingIneligibilityReason.TooFewUniqueContributors,
                10),
        };

        ParkItemRatingRankingDto dto = result.ToHttp();

        Assert.Equal(3, dto.Rank);
        Assert.Equal("Talocan", dto.TargetName);
        Assert.Equal("park-1", dto.ParkId);
        Assert.Equal("Phantasialand", dto.ParkName);
        Assert.Equal("FlatRide", dto.ParkItemType);
        Assert.Equal(7, dto.RatingObservationCount);
        Assert.Equal(7, dto.UniqueContributorCount);
        Assert.Equal("Provisional", dto.Evidence?.Level);
        Assert.False(dto.Evidence?.IsEligibleForMainRanking);
        Assert.Equal("TooFewUniqueContributors", dto.Evidence?.IneligibilityReason);
        Assert.Equal(10, dto.Evidence?.NextThreshold);
        Assert.Equal("ratings-2026-01", dto.MethodologyVersion);
    }
}
