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

public sealed class SharedPassportProfilesControllerTests
{
    [Fact]
    public async Task GetAsync_ShouldExposeBusinessLabelsWithoutInternalIdentifiers()
    {
        PassportProfileSharePreviewResult content = new PassportProfileSharePreviewResult(
            "Alex",
            null,
            null,
            ShareVisibility.Unlisted,
            true,
            1,
            1,
            2,
            2,
            null,
            null,
            new[] { new PassportProfileShareCountryResult("FR", 1, 1) },
            new[] { new PassportProfileShareYearResult(2026, 1, 1, 2) },
            new[] { new PassportProfileShareParkResult("Denain Évasion", "FR", 1, 2026, 2026, 2, null) },
            new[] { new PassportProfileShareRatingResult("Park", "Denain Évasion", null, null, 5) },
            Array.Empty<PassportProfileShareMissedItemResult>(),
            false,
            "passport-profile-v1",
            false);
        SharedPassportProfileResult shared = new SharedPassportProfileResult(
            new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc),
            content);
        Mock<IQueryHandler<GetSharedPassportProfileQuery, ApplicationResult<SharedPassportProfileResult>>> handler =
            new Mock<IQueryHandler<GetSharedPassportProfileQuery, ApplicationResult<SharedPassportProfileResult>>>(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<GetSharedPassportProfileQuery>(query => query.ShareId == "opaque-share-id"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<SharedPassportProfileResult>.Success(shared));
        SharedPassportProfilesController controller = new SharedPassportProfilesController(handler.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        IActionResult result = await controller.GetAsync("opaque-share-id", CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        SharedPassportProfileDto response = Assert.IsType<SharedPassportProfileDto>(ok.Value);
        Assert.Equal("Denain Évasion", Assert.Single(response.PassportProfile.Parks).Name);
        Assert.Equal("no-referrer", controller.Response.Headers["Referrer-Policy"]);
        Assert.Null(typeof(PassportProfileSharePreviewDto).GetProperty("OwnerUserId"));
        Assert.Null(typeof(PassportProfileShareParkDto).GetProperty("ParkId"));
        Assert.Null(typeof(PassportProfileShareRatingDto).GetProperty("SelectionKey"));
        handler.VerifyAll();
    }

    [Fact]
    public void GetEndpoint_ShouldBeAnonymousAndNeverCached()
    {
        MethodInfo action = typeof(SharedPassportProfilesController).GetMethod(
            nameof(SharedPassportProfilesController.GetAsync))
            ?? throw new InvalidOperationException("Get action not found.");

        Assert.NotNull(action.GetCustomAttribute<AllowAnonymousAttribute>());
        ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
            action.GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);
        Assert.Equal(ResponseCacheLocation.None, cache.Location);
    }
}
