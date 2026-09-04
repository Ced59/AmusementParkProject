using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Passport;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class PassportRideTargetsControllerTests
{
    [Fact]
    public async Task ValidateAsync_ShouldForwardTheAuthenticatedOwnerScope()
    {
        Mock<IQueryHandler<ValidateRideTargetsQuery, ApplicationResult<bool>>> handler =
            new Mock<IQueryHandler<ValidateRideTargetsQuery, ApplicationResult<bool>>>(
                MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<ValidateRideTargetsQuery>(query =>
                    query.UserId == "owner-1"
                    && query.ParkId == "park-1"
                    && query.ParkItemIds.SequenceEqual(new[] { "item-1" })),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<bool>.Success(true));
        PassportRideTargetsController controller =
            new PassportRideTargetsController(handler.Object)
            {
                ControllerContext = CreateControllerContext(),
            };

        IActionResult result = await controller.ValidateAsync(
            new ValidatePassportRideTargetsRequestDto
            {
                ParkId = "park-1",
                ParkItemIds = new[] { "item-1" },
            },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        handler.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldRemainPrivateAndNonCacheable()
    {
        RouteAttribute? route =
            typeof(PassportRideTargetsController).GetCustomAttribute<RouteAttribute>();
        Assert.Equal("me/passport/ride-targets", route?.Template);
        AuthorizeAttribute authorize = Assert.Single(
            typeof(PassportRideTargetsController).GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.UserModeratorAdmin, authorize.Roles);
        Assert.NotNull(typeof(PassportRideTargetsController)
            .GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        ResponseCacheAttribute? cache =
            typeof(PassportRideTargetsController).GetCustomAttribute<ResponseCacheAttribute>();
        Assert.True(cache?.NoStore);
        HttpPostAttribute? post = typeof(PassportRideTargetsController)
            .GetMethod(nameof(PassportRideTargetsController.ValidateAsync))
            ?.GetCustomAttribute<HttpPostAttribute>();
        Assert.Equal(":validate", post?.Template);
    }

    private static ControllerContext CreateControllerContext()
    {
        ClaimsIdentity identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "owner-1") },
            "Test");
        DefaultHttpContext context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity),
        };
        return new ControllerContext { HttpContext = context };
    }
}
