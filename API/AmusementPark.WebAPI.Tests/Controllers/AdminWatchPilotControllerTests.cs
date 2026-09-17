using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Queries;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Watchlists;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class AdminWatchPilotControllerTests
{
    [Fact]
    public async Task GetMetricsAsync_ReturnsOnlyAggregatePilotData()
    {
        DateTime fromUtc = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime toUtc = new(2026, 9, 17, 23, 59, 59, DateTimeKind.Utc);
        WatchPilotMetricsResult metrics = new(
            toUtc,
            fromUtc,
            toUtc,
            12,
            new Dictionary<string, long> { ["ParkNameChanged"] = 7 },
            8,
            7,
            1,
            0,
            6,
            0,
            5,
            4,
            1,
            2,
            3,
            0,
            5,
            1,
            0,
            null,
            null,
            0,
            new Dictionary<string, long> { ["Pending"] = 1 },
            new WatchPilotHealth(0m, 1m, 5m, 2m, WatchPilotSignal.Monitor, false, false),
            []);
        Mock<IQueryHandler<GetWatchPilotMetricsQuery,
            ApplicationResult<WatchPilotMetricsResult>>> handler = new(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                new GetWatchPilotMetricsQuery(fromUtc, toUtc),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<WatchPilotMetricsResult>.Success(metrics));
        AdminWatchPilotController controller = new(handler.Object);

        IActionResult response = await controller.GetMetricsAsync(
            fromUtc,
            toUtc,
            CancellationToken.None);

        WatchPilotMetricsDto body = Assert.IsType<WatchPilotMetricsDto>(
            Assert.IsType<OkObjectResult>(response).Value);
        Assert.Equal(12, body.ActiveSubscriptions);
        Assert.Equal(1, body.MisleadingAlertReports);
        Assert.False(body.Health.ProviderFeedbackAvailable);
        Assert.Null(typeof(WatchPilotMetricsDto).GetProperty("UserId"));
        Assert.Null(typeof(WatchPilotMetricsDto).GetProperty("NotificationId"));
        handler.VerifyAll();
    }

    [Fact]
    public void Controller_IsAdminOnlyNoStoreAndRateLimited()
    {
        Type controllerType = typeof(AdminWatchPilotController);
        AuthorizeAttribute authorize = Assert.Single(
            controllerType.GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.Admin, authorize.Roles);
        Assert.NotNull(controllerType.GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        Assert.True(Assert.IsType<ResponseCacheAttribute>(
            controllerType.GetCustomAttribute<ResponseCacheAttribute>()).NoStore);
        MethodInfo? action = controllerType.GetMethod("GetMetricsAsync");
        Assert.NotNull(action);
        EnableRateLimitingAttribute rateLimit = Assert.IsType<EnableRateLimitingAttribute>(
            action.GetCustomAttribute<EnableRateLimitingAttribute>());
        Assert.Equal(RateLimitPolicyNames.FactualEventAdministration, rateLimit.PolicyName);
    }
}
