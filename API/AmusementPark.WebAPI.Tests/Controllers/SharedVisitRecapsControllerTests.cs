using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.WebAPI.Contracts.Sharing;
using AmusementPark.WebAPI.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class SharedVisitRecapsControllerTests
{
    [Fact]
    public async Task GetAsync_ShouldReturnTheFrozenPublicContractAndDisableReferrers()
    {
        SharedVisitRecapResult recap = new SharedVisitRecapResult(
            new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc),
            new VisitRecapSharePreviewResult(
                "park-1",
                "Denain Évasion",
                new VisitRecapShareDateResult(
                    2026,
                    7,
                    null,
                    ShareDatePrecision.Month,
                    false),
                1,
                2,
                new[] { "Attraction" },
                4.5,
                null,
                null,
                new[]
                {
                    new VisitRecapShareItemResult(
                        "item-1",
                        "Le Galion",
                        "Attraction",
                        2,
                        4.5,
                        false),
                },
                "Un beau souvenir",
                false,
                false,
                false));
        Mock<IQueryHandler<GetSharedVisitRecapQuery, ApplicationResult<SharedVisitRecapResult>>> handler =
            new Mock<IQueryHandler<GetSharedVisitRecapQuery, ApplicationResult<SharedVisitRecapResult>>>(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<GetSharedVisitRecapQuery>(query => query.ShareId == "opaque-share-id"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<SharedVisitRecapResult>.Success(recap));
        SharedVisitRecapsController controller = new SharedVisitRecapsController(handler.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };

        IActionResult result = await controller.GetAsync(
            "opaque-share-id",
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        SharedVisitRecapDto response = Assert.IsType<SharedVisitRecapDto>(ok.Value);
        Assert.Equal("Denain Évasion", response.VisitRecap.ParkName);
        Assert.Equal("Le Galion", Assert.Single(response.VisitRecap.Items).Name);
        Assert.Equal("no-referrer", controller.Response.Headers["Referrer-Policy"]);
        Assert.Null(typeof(SharedVisitRecapDto).GetProperty("OwnerUserId"));
        Assert.Null(typeof(SharedVisitRecapItemDto).GetProperty("ParkItemId"));
        handler.VerifyAll();
    }

    [Fact]
    public void GetEndpoint_ShouldBeAnonymousAndNeverCached()
    {
        MethodInfo action = typeof(SharedVisitRecapsController).GetMethod(
            nameof(SharedVisitRecapsController.GetAsync))
            ?? throw new InvalidOperationException("Get action not found.");

        Assert.NotNull(action.GetCustomAttribute<AllowAnonymousAttribute>());
        ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
            action.GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);
        Assert.Equal(ResponseCacheLocation.None, cache.Location);
    }
}
