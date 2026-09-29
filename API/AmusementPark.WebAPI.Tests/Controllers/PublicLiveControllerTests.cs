using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Queries;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class PublicLiveControllerTests
{
    [Fact]
    public async Task GetParkAsync_ShouldReturnAttributionAndEntityTag()
    {
        PublicLiveTargetResult target = CreateTarget();
        PublicLiveController controller = CreateController(target);

        IActionResult result = await controller.GetParkAsync("park-1", CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        AmusementPark.WebAPI.Contracts.LiveData.PublicLiveTargetDto dto =
            Assert.IsType<AmusementPark.WebAPI.Contracts.LiveData.PublicLiveTargetDto>(ok.Value);
        Assert.Equal("Powered by ThemeParks.wiki", dto.Source?.AttributionText);
        Assert.Equal(0, Assert.Single(dto.Queues).WaitTimeMinutes);
        Assert.StartsWith("\"", controller.Response.Headers.ETag.ToString(), StringComparison.Ordinal);
        Assert.Equal("public,max-age=0,must-revalidate", controller.Response.Headers.CacheControl);
        DateTime transition = Assert.IsType<DateTime>(
            controller.HttpContext.Items[
                PublicLiveExpirationOutputCachePolicy.FreshnessTransitionItemKey]);
        Assert.Equal(target.FreshnessTransitionAtUtc, transition);
    }

    [Fact]
    public async Task GetParkAsync_WhenEntityTagMatches_ShouldReturnNotModified()
    {
        PublicLiveTargetResult target = CreateTarget();
        PublicLiveController controller = CreateController(target);
        string entityTag = PublicLiveEntityTagFactory.Create(target.ToHttp());
        controller.Request.Headers.IfNoneMatch = $"W/{entityTag}";

        IActionResult result = await controller.GetParkAsync("park-1", CancellationToken.None);

        StatusCodeResult status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status304NotModified, status.StatusCode);
    }

    private static PublicLiveController CreateController(PublicLiveTargetResult target)
    {
        Mock<IQueryHandler<GetPublicParkLiveQuery, ApplicationResult<PublicLiveTargetResult>>> park =
            new Mock<IQueryHandler<GetPublicParkLiveQuery, ApplicationResult<PublicLiveTargetResult>>>(
                MockBehavior.Strict);
        Mock<IQueryHandler<GetPublicParkItemLiveQuery, ApplicationResult<PublicLiveTargetResult>>> item =
            new Mock<IQueryHandler<GetPublicParkItemLiveQuery, ApplicationResult<PublicLiveTargetResult>>>(
                MockBehavior.Strict);
        Mock<IQueryHandler<GetPublicParkLiveItemsQuery, ApplicationResult<PublicParkLiveItemsResult>>> items =
            new Mock<IQueryHandler<GetPublicParkLiveItemsQuery, ApplicationResult<PublicParkLiveItemsResult>>>(
                MockBehavior.Strict);
        park.Setup(handler => handler.HandleAsync(
                It.Is<GetPublicParkLiveQuery>(query => query.ParkId == "park-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<PublicLiveTargetResult>.Success(target));
        PublicLiveController controller = new PublicLiveController(
            park.Object,
            item.Object,
            items.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };
        return controller;
    }

    private static PublicLiveTargetResult CreateTarget()
    {
        DateTime asOfUtc = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
        return new PublicLiveTargetResult(
            "park-1",
            LiveTargetType.Park,
            "Park",
            "park-1",
            "Park",
            PublicLiveAvailability.Current,
            LiveOperationalStatus.Open,
            new[]
            {
                new PublicLiveQueueResult(
                    LiveQueueKind.Standby,
                    0,
                    false,
                    LiveQueueAvailability.Unspecified,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null),
            },
            asOfUtc,
            asOfUtc.AddMinutes(-2),
            asOfUtc.AddMinutes(-2).AddSeconds(1),
            120,
            LiveFreshnessState.Fresh,
            asOfUtc.AddMinutes(28),
            asOfUtc.AddMinutes(8),
            new PublicLiveSourceResult(
                "themeparks-wiki",
                "ThemeParks.wiki",
                LiveDataSourceType.AuthorizedAggregator,
                "Powered by ThemeParks.wiki",
                "https://themeparks.wiki/"),
            LiveDataConfidence.Medium);
    }
}
