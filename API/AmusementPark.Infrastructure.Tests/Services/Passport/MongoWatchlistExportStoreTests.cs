using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Services.Passport;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Passport;

public sealed class MongoWatchlistExportStoreTests
{
    [Fact]
    public void MapTargets_ShouldUseOnlyPublicProjectedTargetsAndDomainStatuses()
    {
        BsonDocument park = new BsonDocument
        {
            ["_id"] = "park-1",
            ["name"] = "Parc exemple",
            ["status"] = "Operating",
            ["isVisible"] = true,
            ["adminReviewStatus"] = "Validated",
        };
        BsonDocument hiddenPark = new BsonDocument
        {
            ["_id"] = "park-hidden",
            ["name"] = "Parc caché",
            ["status"] = "Operating",
            ["isVisible"] = false,
            ["adminReviewStatus"] = "Validated",
        };
        BsonDocument closedAttraction = new BsonDocument
        {
            ["_id"] = "item-1",
            ["parkId"] = "park-1",
            ["name"] = "Ancienne attraction",
            ["category"] = "Attraction",
            ["isVisible"] = true,
            ["attractionDetails"] = new BsonDocument("status", "Removed"),
        };

        PassportWatchlistTargetCatalog result = MongoWatchlistExportStore.MapTargets(
            new[] { "park-1", "park-hidden", "park-missing" },
            new[] { "item-1", "item-missing" },
            new[] { park, hiddenPark },
            new[] { closedAttraction });

        Assert.Equal("Parc exemple", result.ParkTargets["park-1"].Name);
        Assert.Null(result.ParkTargets["park-hidden"].Name);
        Assert.Null(result.ParkTargets["park-missing"].Name);
        Assert.Equal("Ancienne attraction", result.ParkItemTargets["item-1"].Name);
        Assert.Equal("Parc exemple", result.ParkItemTargets["item-1"].ParkName);
        Assert.Equal(
            CollectionTargetStatus.PermanentlyClosed,
            result.ParkItemTargets["item-1"].Status);
        Assert.Null(result.ParkItemTargets["item-missing"].Name);
    }
}
