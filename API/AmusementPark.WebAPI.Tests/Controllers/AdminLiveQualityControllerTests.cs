using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Commands;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.LiveData;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class AdminLiveQualityControllerTests
{
    [Fact]
    public void Controller_ShouldProtectAndAuditReplay()
    {
        Type type = typeof(AdminLiveQualityController);

        Assert.Equal("admin/live/quality", type.GetCustomAttribute<RouteAttribute>()?.Template);
        AuthorizeAttribute authorize = Assert.Single(
            type.GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.Admin, authorize.Roles);
        Assert.NotNull(type.GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        Assert.True(type.GetCustomAttribute<ResponseCacheAttribute>()?.NoStore);
        MethodInfo replay = type.GetMethod(nameof(AdminLiveQualityController.ReplayAsync))!;
        Assert.Equal(
            RateLimitPolicyNames.LiveDataAdministration,
            replay.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);
        Assert.NotNull(replay.GetCustomAttribute<AdminAuditAttribute>());
    }

    [Fact]
    public async Task ReplayAsync_ShouldUseAuthenticatedAdministratorAndReturnCounters()
    {
        Mock<ICommandHandler<
            ReplayLiveQualityIncidentsCommand,
            ApplicationResult<LiveQualityReplayResult>>> handler = new(MockBehavior.Strict);
        handler
            .Setup(value => value.HandleAsync(
                It.Is<ReplayLiveQualityIncidentsCommand>(command =>
                    command.MaximumCount == 10
                    && command.AdministratorUserId == "admin-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<LiveQualityReplayResult>.Success(
                new LiveQualityReplayResult(10, 7, 3, 6, 1)));
        AdminLiveQualityController controller = new AdminLiveQualityController(
            handler.Object,
            CreateMutationAvailability(true).Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.NameIdentifier, "admin-1") },
                            "test")),
                },
            },
        };

        IActionResult action = await controller.ReplayAsync(
            new ReplayLiveQualityIncidentsRequestDto { MaximumCount = 10 },
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(action);
        LiveQualityReplayDto response = Assert.IsType<LiveQualityReplayDto>(ok.Value);
        Assert.Equal(7, response.ResolvedCount);
        Assert.Equal(3, response.StillBlockedCount);
        handler.VerifyAll();
    }

    [Fact]
    public async Task ReplayAsync_ShouldFailClosedDuringDeploymentOverlap()
    {
        Mock<ICommandHandler<
            ReplayLiveQualityIncidentsCommand,
            ApplicationResult<LiveQualityReplayResult>>> handler = new(MockBehavior.Strict);
        AdminLiveQualityController controller = new AdminLiveQualityController(
            handler.Object,
            CreateMutationAvailability(false).Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };

        IActionResult action = await controller.ReplayAsync(
            new ReplayLiveQualityIncidentsRequestDto { MaximumCount = 10 },
            CancellationToken.None);

        ObjectResult unavailable = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        Assert.Equal("30", controller.Response.Headers.RetryAfter);
        handler.VerifyNoOtherCalls();
    }

    private static Mock<ILiveOperationalMutationAvailability> CreateMutationAvailability(
        bool enabled)
    {
        Mock<ILiveOperationalMutationAvailability> availability = new(MockBehavior.Strict);
        availability.SetupGet(value => value.IsEnabled).Returns(enabled);
        return availability;
    }
}
