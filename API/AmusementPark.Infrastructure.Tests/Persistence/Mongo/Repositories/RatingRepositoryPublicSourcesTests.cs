using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class RatingRepositoryPublicSourcesTests
{
    [Fact]
    public void IsPublicUserRatingSource_ShouldRejectHiddenParkRatings()
    {
        UserRatingDocument rating = CreateRating(RatingTargetType.Park, "park-1", "park-1");
        Dictionary<string, ParkDocument> parks = new Dictionary<string, ParkDocument>
        {
            ["park-1"] = new ParkDocument
            {
                Id = "park-1",
                IsVisible = false,
                Status = ParkStatus.Operating,
            },
        };

        bool result = RatingRepository.IsPublicUserRatingSource(
            rating,
            parks,
            new Dictionary<string, ParkItemDocument>());

        Assert.False(result);
    }

    [Fact]
    public void IsPublicUserRatingSource_ShouldRejectItemsWhoseParentParkIsNotPublic()
    {
        UserRatingDocument rating = CreateRating(RatingTargetType.ParkItem, "item-1", "park-1");
        Dictionary<string, ParkDocument> parks = new Dictionary<string, ParkDocument>();
        Dictionary<string, ParkItemDocument> items = new Dictionary<string, ParkItemDocument>
        {
            ["item-1"] = new ParkItemDocument
            {
                Id = "item-1",
                ParkId = "park-1",
                IsVisible = true,
                Category = ParkItemCategory.Attraction,
                Type = ParkItemType.FlatRide,
            },
        };

        bool result = RatingRepository.IsPublicUserRatingSource(rating, parks, items);

        Assert.False(result);
    }

    [Fact]
    public void IsPublicUserRatingSource_ShouldAllowAVisibleCurrentItemAndParentPark()
    {
        UserRatingDocument rating = CreateRating(RatingTargetType.ParkItem, "item-1", "park-1");
        Dictionary<string, ParkDocument> parks = new Dictionary<string, ParkDocument>
        {
            ["park-1"] = new ParkDocument
            {
                Id = "park-1",
                IsVisible = true,
                Status = ParkStatus.Operating,
            },
        };
        Dictionary<string, ParkItemDocument> items = new Dictionary<string, ParkItemDocument>
        {
            ["item-1"] = new ParkItemDocument
            {
                Id = "item-1",
                ParkId = "park-1",
                IsVisible = true,
                Category = ParkItemCategory.Attraction,
                Type = ParkItemType.FlatRide,
                AttractionDetails = new AttractionDetailsDocument { Status = "Operating" },
            },
        };

        bool result = RatingRepository.IsPublicUserRatingSource(rating, parks, items);

        Assert.True(result);
    }

    [Fact]
    public void SelectPublicUserRatingSources_ShouldApplyLimitAfterHiddenRatingsAreRemoved()
    {
        UserRatingDocument hidden = CreateRating(RatingTargetType.Park, "park-hidden", "park-hidden");
        UserRatingDocument firstVisible = CreateRating(RatingTargetType.Park, "park-visible-1", "park-visible-1");
        UserRatingDocument secondVisible = CreateRating(RatingTargetType.Park, "park-visible-2", "park-visible-2");
        Dictionary<string, ParkDocument> parks = new Dictionary<string, ParkDocument>
        {
            ["park-hidden"] = new ParkDocument
            {
                Id = "park-hidden",
                IsVisible = false,
                Status = ParkStatus.Operating,
            },
            ["park-visible-1"] = new ParkDocument
            {
                Id = "park-visible-1",
                IsVisible = true,
                Status = ParkStatus.Operating,
            },
            ["park-visible-2"] = new ParkDocument
            {
                Id = "park-visible-2",
                IsVisible = true,
                Status = ParkStatus.Operating,
            },
        };

        IReadOnlyCollection<UserRatingDocument> result = RatingRepository.SelectPublicUserRatingSources(
            new[] { hidden, firstVisible, secondVisible },
            parks,
            new Dictionary<string, ParkItemDocument>(),
            2);

        Assert.Equal(new[] { "park-visible-1", "park-visible-2" }, result.Select(static rating => rating.TargetId));
    }

    [Fact]
    public void CurrentMetadataResolvers_ShouldIgnoreCachedRatingMetadataAfterItemChanges()
    {
        UserRatingDocument rating = CreateRating(
            RatingTargetType.ParkItem,
            "item-1",
            "park-old");
        rating.ParkItemCategory = ParkItemCategory.Attraction;
        rating.ParkItemType = ParkItemType.RollerCoaster;
        Dictionary<string, ParkItemDocument> items = new Dictionary<string, ParkItemDocument>
        {
            ["item-1"] = new ParkItemDocument
            {
                Id = "item-1",
                ParkId = "park-current",
                Category = ParkItemCategory.Restaurant,
                Type = ParkItemType.Restaurant,
            },
        };

        string parkId = RatingRepository.ResolveCurrentParkId(rating, items);
        ParkItemCategory? category = RatingRepository.ResolveCurrentParkItemCategory(rating, items);
        ParkItemType? type = RatingRepository.ResolveCurrentParkItemType(rating, items);

        Assert.Equal("park-current", parkId);
        Assert.Equal(ParkItemCategory.Restaurant, category);
        Assert.Equal(ParkItemType.Restaurant, type);
    }

    [Fact]
    public void ResolveTargetName_WhenPublicMetadataIsMissing_ShouldNotExposeTargetId()
    {
        UserRatingDocument rating = CreateRating(
            RatingTargetType.ParkItem,
            "technical-item-id",
            "technical-park-id");

        string result = RatingRepository.ResolveTargetName(
            rating,
            null,
            new Dictionary<string, ParkItemDocument>(),
            hideTechnicalFallbacks: true);

        Assert.Empty(result);
    }

    private static UserRatingDocument CreateRating(
        RatingTargetType targetType,
        string targetId,
        string parkId)
    {
        return new UserRatingDocument
        {
            UserId = "owner-1",
            TargetType = targetType,
            TargetId = targetId,
            ParkId = parkId,
            Value = 4.5d,
        };
    }
}
