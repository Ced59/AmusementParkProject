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
    [Theory]
    [InlineData(NearbyDestinationKind.Park)]
    [InlineData(NearbyDestinationKind.StandaloneAttraction)]
    public async Task GetAsync_ReturnsMixedPublicTargetsInRankOrder(NearbyDestinationKind kind)
    {
        Mock<IQueryHandler<GetNearbyDestinationsQuery, ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>>>> handler =
            new Mock<IQueryHandler<GetNearbyDestinationsQuery, ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>>>>(MockBehavior.Strict);
        NearbyDestinationResult[] targets =
        {
            new NearbyDestinationResult(null, new StandaloneAttraction { Id = "attraction", Name = "Luge", IsVisible = true }, 1.5, 5, 1),
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
        Assert.Equal("park", response[1].Park!.Id);
        Assert.Null(response[1].StandaloneAttraction);
        Assert.Equal(new[] { 1, 2 }, response.Select(target => target.ProximityRank));
        Assert.Equal(new[] { 1.5, 2.5 }, response.Select(target => target.DistanceKilometers));
        handler.VerifyAll();
    }
}
