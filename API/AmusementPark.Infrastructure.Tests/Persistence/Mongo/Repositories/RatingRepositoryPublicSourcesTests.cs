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
