using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.OutputCaching;

public sealed class SsrPageCacheInvalidationRequestResolverTests
{
    [Fact]
    public async Task ResolveAsync_ForParkUpdate_ShouldTargetParkAndDiscoveryPages()
    {
        SsrPageCacheInvalidationRequestResolver resolver = CreateResolver();
        ActionExecutingContext context = CreateContext("Parks", new Dictionary<string, object?> { ["id"] = "park-1" });

        AmusementPark.Application.Ports.SsrPageCacheInvalidationRequest request = await resolver.ResolveAsync(
            context,
            null,
            new[] { PublicCacheScope.Data },
            CancellationToken.None);

        Assert.False(request.All);
        Assert.Contains("/fr/park/park-1/", request.Prefixes);
        Assert.Contains("/fr/home", request.Paths);
        Assert.True(request.IncludeSeoDocuments);
    }

    [Fact]
    public async Task ResolveAsync_ForParkItemDeleteBeforeExecution_ShouldTargetParentPark()
    {
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Contains("item-1")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new ParkItem { Id = "item-1", ParkId = "park-1" } });

        SsrPageCacheInvalidationRequestResolver resolver = CreateResolver(parkItemRepository: parkItemRepository);
        ActionExecutingContext context = CreateContext("ParkItems", new Dictionary<string, object?> { ["id"] = "item-1" });

        AmusementPark.Application.Ports.SsrPageCacheInvalidationRequest request = await resolver.ResolveAsync(
            context,
            null,
            new[] { PublicCacheScope.Data },
            CancellationToken.None);

        Assert.False(request.All);
        Assert.Contains("/fr/park/park-1/", request.Prefixes);
        Assert.DoesNotContain("/fr/home", request.Paths);
        parkItemRepository.VerifyAll();
    }

    [Fact]
    public async Task ResolveAsync_ForManufacturerUpdate_ShouldTargetReferenceAndAffectedParks()
    {
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetParkIdsByManufacturerIdAsync("manufacturer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "park-1", "park-2" });

        SsrPageCacheInvalidationRequestResolver resolver = CreateResolver(parkItemRepository: parkItemRepository);
        ActionExecutingContext context = CreateContext("AttractionManufacturers", new Dictionary<string, object?> { ["id"] = "manufacturer-1" });

        AmusementPark.Application.Ports.SsrPageCacheInvalidationRequest request = await resolver.ResolveAsync(
            context,
            null,
            new[] { PublicCacheScope.ReferenceData, PublicCacheScope.Data },
            CancellationToken.None);

        Assert.False(request.All);
        Assert.Contains("/fr/park-manufacturer/manufacturer-1/", request.Prefixes);
        Assert.Contains("/fr/park/park-1/", request.Prefixes);
        Assert.Contains("/fr/park/park-2/", request.Prefixes);
        parkItemRepository.VerifyAll();
    }

    private static SsrPageCacheInvalidationRequestResolver CreateResolver(
        Mock<IParkRepository>? parkRepository = null,
        Mock<IParkItemRepository>? parkItemRepository = null,
        Mock<IParkZoneRepository>? parkZoneRepository = null,
        Mock<IImageRepository>? imageRepository = null)
    {
        return new SsrPageCacheInvalidationRequestResolver(
            (parkRepository ?? new Mock<IParkRepository>(MockBehavior.Strict)).Object,
            (parkItemRepository ?? new Mock<IParkItemRepository>(MockBehavior.Strict)).Object,
            (parkZoneRepository ?? new Mock<IParkZoneRepository>(MockBehavior.Strict)).Object,
            (imageRepository ?? new Mock<IImageRepository>(MockBehavior.Strict)).Object);
    }

    private static ActionExecutingContext CreateContext(string controllerName, IDictionary<string, object?> routeValues)
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        RouteData routeData = new RouteData();

        foreach (KeyValuePair<string, object?> routeValue in routeValues)
        {
            routeData.Values[routeValue.Key] = routeValue.Value;
        }

        ControllerActionDescriptor descriptor = new ControllerActionDescriptor
        {
            ControllerName = controllerName,
        };

        ActionContext actionContext = new ActionContext(httpContext, routeData, descriptor);

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }
}
