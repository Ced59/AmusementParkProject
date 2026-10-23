using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Application.Features.NearbyDestinations.Results;
using AmusementPark.Application.Features.NearbyDestinations.Handlers;
using AmusementPark.Application.Features.NearbyDestinations.Queries;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.NearbyDestinations;

public sealed class GetNearbyDestinationsQueryHandlerTests
{
    [Theory]
    [InlineData(NearbyDestinationKind.Park)]
    [InlineData(NearbyDestinationKind.StandaloneAttraction)]
    public async Task HandleAsync_MergesBothKindsBeforeLimitingAndExcludesOnlyTheSource(NearbyDestinationKind sourceKind)
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IStandaloneAttractionRepository> attractions = new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        SetupSource(parks, attractions, sourceKind);
        parks.Setup(repository => repository.GetNearestByLocationAsync(49, 0, 4, null, false, ClosedEntityFilter.OpenOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Park("source", 0), Park("park-near", 0.02), Park("park-far", 0.2) });
        attractions.Setup(repository => repository.GetNearestByLocationAsync(49, 0, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Attraction("source", 0), Attraction("attraction-near", 0.01), Attraction("attraction-far", 0.03) });

        GetNearbyDestinationsQueryHandler handler = new GetNearbyDestinationsQueryHandler(parks.Object, attractions.Object, EmptyImages());
        ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>> result = await handler.HandleAsync(new GetNearbyDestinationsQuery(sourceKind, "source", 3));

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "source", "attraction-near", "park-near" },
            result.Value!.Select(target => target.Park?.Id ?? target.StandaloneAttraction!.Id));
        Assert.Equal(new[] { 1, 2, 3 }, result.Value!.Select(target => target.ProximityRank));
        Assert.Equal(sourceKind == NearbyDestinationKind.Park, result.Value!.First().StandaloneAttraction is not null);
        Assert.All(result.Value!, target => Assert.True(target.DistanceKilometers >= 0));
        parks.VerifyAll();
        attractions.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_DoesNotReservePlacesForParks()
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IStandaloneAttractionRepository> attractions = new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        SetupSource(parks, attractions, NearbyDestinationKind.Park);
        parks.Setup(repository => repository.GetNearestByLocationAsync(49, 0, 5, null, false, ClosedEntityFilter.OpenOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Park("source", 0), Park("far", 1) });
        attractions.Setup(repository => repository.GetNearestByLocationAsync(49, 0, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Attraction("a", 0.01), Attraction("b", 0.02), Attraction("c", 0.03), Attraction("d", 0.04), Attraction("e", 0.05) });

        ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>> result = await new GetNearbyDestinationsQueryHandler(parks.Object, attractions.Object, EmptyImages())
            .HandleAsync(new GetNearbyDestinationsQuery(NearbyDestinationKind.Park, "source"));

        Assert.Equal(new[] { "a", "b", "c", "d" }, result.Value!.Select(target => target.StandaloneAttraction!.Id));
    }

    [Fact]
    public async Task HandleAsync_ExcludesNonPublicOrUnlocatedCandidates()
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IStandaloneAttractionRepository> attractions = new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        SetupSource(parks, attractions, NearbyDestinationKind.StandaloneAttraction);
        Park hiddenPark = Park("hidden-park", 0);
        hiddenPark.IsVisible = false;
        StandaloneAttraction hidden = Attraction("hidden", 0);
        hidden.IsVisible = false;
        StandaloneAttraction excluded = Attraction("excluded", 0);
        excluded.AdminReviewStatus = AdminReviewStatus.NotRelevant;
        StandaloneAttraction closed = Attraction("closed", 0);
        closed.AttractionDetails = new AttractionDetails { Status = "ClosedDefinitively" };
        StandaloneAttraction unlocated = Attraction("unlocated", 0);
        unlocated.ClearPosition();
        parks.Setup(repository => repository.GetNearestByLocationAsync(49, 0, 5, null, false, ClosedEntityFilter.OpenOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { hiddenPark, Park("visible", 0.1) });
        attractions.Setup(repository => repository.GetNearestByLocationAsync(49, 0, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { hidden, excluded, closed, unlocated });

        ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>> result = await new GetNearbyDestinationsQueryHandler(parks.Object, attractions.Object, EmptyImages())
            .HandleAsync(new GetNearbyDestinationsQuery(NearbyDestinationKind.StandaloneAttraction, "source"));

        Assert.Equal("visible", Assert.Single(result.Value!).Park!.Id);
    }

    [Fact]
    public async Task HandleAsync_WithoutCoordinatesDoesNotQueryCandidates()
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IStandaloneAttractionRepository> attractions = new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        Park source = Park("source", 0);
        source.ClearPosition();
        parks.Setup(repository => repository.GetByIdAsync("source", false, It.IsAny<CancellationToken>())).ReturnsAsync(source);

        ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>> result = await new GetNearbyDestinationsQueryHandler(parks.Object, attractions.Object, EmptyImages())
            .HandleAsync(new GetNearbyDestinationsQuery(NearbyDestinationKind.Park, "source"));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Theory]
    [InlineData(NearbyDestinationKind.Park)]
    [InlineData(NearbyDestinationKind.StandaloneAttraction)]
    public async Task HandleAsync_MissingPublicSourceDoesNotQueryCandidates(NearbyDestinationKind kind)
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IStandaloneAttractionRepository> attractions = new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        if (kind == NearbyDestinationKind.Park)
        {
            parks.Setup(repository => repository.GetByIdAsync("missing", false, It.IsAny<CancellationToken>())).ReturnsAsync((Park?)null);
        }
        else
        {
            attractions.Setup(repository => repository.GetByIdAsync("missing", false, It.IsAny<CancellationToken>())).ReturnsAsync((StandaloneAttraction?)null);
        }

        ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>> result = await new GetNearbyDestinationsQueryHandler(parks.Object, attractions.Object, EmptyImages())
            .HandleAsync(new GetNearbyDestinationsQuery(kind, "missing"));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_LoadsOnlyPublishedPhotosForTheSelectedStandaloneOwnersInOneBatch()
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IStandaloneAttractionRepository> attractions = new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        SetupSource(parks, attractions, NearbyDestinationKind.Park);
        parks.Setup(repository => repository.GetNearestByLocationAsync(49, 0, 4, null, false, ClosedEntityFilter.OpenOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Park("source", 0), Park("near-park", 0.02) });
        attractions.Setup(repository => repository.GetNearestByLocationAsync(49, 0, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Attraction("photo-owner", 0.01), Attraction("without-photo", 0.03), Attraction("outside-limit", 1) });
        images.Setup(repository => repository.GetMainImageIdsByOwnersAsync(
            ImageOwnerType.StandaloneAttraction,
            It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "photo-owner", "without-photo" })),
            ImageCategory.StandaloneAttraction, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { ["photo-owner"] = "published-photo" });

        ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>> result = await new GetNearbyDestinationsQueryHandler(parks.Object, attractions.Object, images.Object)
            .HandleAsync(new GetNearbyDestinationsQuery(NearbyDestinationKind.Park, "source", 3));

        Assert.Equal(new string?[] { "published-photo", null, null }, result.Value!.Select(target => target.PhotoImageId));
        Assert.Equal(new[] { 1, 2, 3 }, result.Value!.Select(target => target.ProximityRank));
        images.VerifyAll();
        images.VerifyNoOtherCalls();
    }

    private static IImageRepository EmptyImages()
    {
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(repository => repository.GetMainImageIdsByOwnersAsync(
            ImageOwnerType.StandaloneAttraction, It.IsAny<IReadOnlyCollection<string>>(),
            ImageCategory.StandaloneAttraction, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>());
        return images.Object;
    }

    private static void SetupSource(Mock<IParkRepository> parks, Mock<IStandaloneAttractionRepository> attractions, NearbyDestinationKind kind)
    {
        if (kind == NearbyDestinationKind.Park)
        {
            parks.Setup(repository => repository.GetByIdAsync("source", false, It.IsAny<CancellationToken>())).ReturnsAsync(Park("source", 0));
        }
        else
        {
            attractions.Setup(repository => repository.GetByIdAsync("source", false, It.IsAny<CancellationToken>())).ReturnsAsync(Attraction("source", 0));
        }
    }

    private static Park Park(string id, double longitude)
    {
        Park park = new Park { Id = id, Name = id, IsVisible = true };
        park.SetPosition(49, longitude);
        return park;
    }

    private static StandaloneAttraction Attraction(string id, double longitude)
    {
        StandaloneAttraction attraction = new StandaloneAttraction { Id = id, Name = id, IsVisible = true };
        attraction.SetPosition(49, longitude);
        return attraction;
    }
}
