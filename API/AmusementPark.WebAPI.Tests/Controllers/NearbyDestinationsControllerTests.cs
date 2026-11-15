using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.NearbyDestinations.Queries;
using AmusementPark.Application.Features.NearbyDestinations.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.NearbyDestinations;
using AmusementPark.WebAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class NearbyDestinationsControllerTests
{
    [Fact]
    public async Task CardView_PreservesLanguagesAndVisibleFieldsWithoutNestedEntityData()
    {
        Park park = new Park { Id = "park", Name = "Disneyland", CountryCode = "US", City = "Anaheim", CurrentLogoImageId = "logo" };
        park.SetPosition(33.8, -117.9);
        foreach (string language in new[] { "en", "fr", "de", "nl", "it", "es", "pl", "pt" })
        {
            park.Descriptions.Add(new AmusementPark.Core.Localization.LocalizedText(language, "<p>" + new string('a', 200) + "</p>"));
        }
        NearbyDestinationResult[] targets = { new NearbyDestinationResult(park, null, 360.1, 240, 4) };
        Mock<IQueryHandler<GetNearbyDestinationsQuery, ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>>>> handler = new();
        handler.Setup(queryHandler => queryHandler.HandleAsync(It.IsAny<GetNearbyDestinationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>>.Success(targets));
        OkObjectResult result = Assert.IsType<OkObjectResult>(await new NearbyDestinationsController(handler.Object).GetAsync(
            NearbyDestinationKind.Park, "source", cardView: true));
        NearbyDestinationCardDto card = Assert.Single(Assert.IsType<NearbyDestinationCardDto[]>(result.Value));
        Assert.Equal("park", card.Id);
        Assert.Equal("Disneyland", card.Name);
        Assert.Equal("logo", card.LogoImageId);
        Assert.Equal("Operating", card.ParkStatus);
        Assert.Equal(33.8, card.Latitude);
        Assert.Equal(360.1, card.DistanceKilometers);
        Assert.Equal(4, card.ProximityRank);
        Assert.Equal(8, card.ShortDescriptions.Count);
        Assert.All(card.ShortDescriptions, text => Assert.Equal(new string('a', 137) + "...", text.Value));
        string json = System.Text.Json.JsonSerializer.Serialize(card);
        Assert.DoesNotContain("AdminReviewStatus", json);
        Assert.DoesNotContain("AttractionDetails", json);
        Assert.True(json.Length < 2500);
    }

    [Fact]
    public async Task CardView_PreservesStandaloneStatusPhotoAndAddressWithoutAccessConditionPayloads()
    {
        StandaloneAttraction attraction = new StandaloneAttraction
        {
            Id = "ride", Name = "SkyJump", CountryCode = "US", City = "Las Vegas", Street = "Strip",
            AttractionDetails = new AttractionDetails { Status = "Operating" },
        };
        attraction.SetPosition(36.1, -115.1);
        NearbyDestinationResult[] targets = { new NearbyDestinationResult(null, attraction, 1.65, 2, 1, "photo") };
        Mock<IQueryHandler<GetNearbyDestinationsQuery, ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>>>> handler = new();
        handler.Setup(queryHandler => queryHandler.HandleAsync(It.IsAny<GetNearbyDestinationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>>.Success(targets));
        OkObjectResult result = Assert.IsType<OkObjectResult>(await new NearbyDestinationsController(handler.Object).GetAsync(
            NearbyDestinationKind.Park, "source", cardView: true));
        NearbyDestinationCardDto card = Assert.Single(Assert.IsType<NearbyDestinationCardDto[]>(result.Value));
        Assert.Equal("StandaloneAttraction", card.Kind);
        Assert.Equal("Operating", card.AttractionStatus);
        Assert.Null(card.ParkStatus);
        Assert.Equal("photo", card.PhotoImageId);
        Assert.Equal("Strip", card.Street);
        Assert.Equal(36.1, card.Latitude);
    }

    [Theory]
    [InlineData(NearbyDestinationKind.Park)]
    [InlineData(NearbyDestinationKind.StandaloneAttraction)]
    public async Task GetAsync_ReturnsMixedPublicTargetsInRankOrder(NearbyDestinationKind kind)
    {
        Mock<IQueryHandler<GetNearbyDestinationsQuery, ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>>>> handler =
            new Mock<IQueryHandler<GetNearbyDestinationsQuery, ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>>>>(MockBehavior.Strict);
        NearbyDestinationResult[] targets =
        {
            new NearbyDestinationResult(null, new StandaloneAttraction { Id = "attraction", Name = "Luge", IsVisible = true }, 1.5, 5, 1, "published-photo"),
            new NearbyDestinationResult(new Park { Id = "park", Name = "Park", IsVisible = true }, null, 2.5, 10, 2)
        };
        handler.Setup(queryHandler => queryHandler.HandleAsync(new GetNearbyDestinationsQuery(kind, "source", 4), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>>.Success(targets));

        IActionResult result = await new NearbyDestinationsController(handler.Object).GetAsync(kind, "source");

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        List<NearbyDestinationDto> response = Assert.IsType<List<NearbyDestinationDto>>(ok.Value);
        Assert.Equal(new[] { "StandaloneAttraction", "Park" }, response.Select(target => target.Kind));
        Assert.Equal("attraction", response[0].StandaloneAttraction!.Id);
        Assert.Null(response[0].Park);
        Assert.Equal("published-photo", response[0].PhotoImageId);
        Assert.Null(response[1].PhotoImageId);
        Assert.Equal("park", response[1].Park!.Id);
        Assert.Null(response[1].StandaloneAttraction);
        Assert.Equal(new[] { 1, 2 }, response.Select(target => target.ProximityRank));
        Assert.Equal(new[] { 1.5, 2.5 }, response.Select(target => target.DistanceKilometers));
        handler.VerifyAll();
    }
}
