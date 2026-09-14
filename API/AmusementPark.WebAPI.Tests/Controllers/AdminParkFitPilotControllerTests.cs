using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.ParkFit;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class AdminParkFitPilotControllerTests
{
    [Fact]
    public async Task GetMetricsAsync_ShouldReturnOnlyAggregateData()
    {
        DateTime fromUtc = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime toUtc = new(2026, 9, 14, 23, 59, 59, DateTimeKind.Utc);
        ParkFitPilotMetricsResult metrics = new(
            toUtc,
            fromUtc,
            toUtc,
            10,
            8,
            1,
            1,
            3,
            2,
            1,
            1,
            new ParkFitPilotHealth(80m, 12.5m, 12.5m, 37.5m, 25m,
                ParkFitPilotSignal.Encouraging, true),
            new Dictionary<string, long>(),
            new Dictionary<string, long>(),
            new Dictionary<string, long>(),
            new Dictionary<string, long>(),
            new Dictionary<string, long>(),
            new Dictionary<string, long>(),
            new Dictionary<string, long>(),
            new Dictionary<string, long>(),
            []);
        Mock<IQueryHandler<
            GetParkFitPilotMetricsQuery,
            ApplicationResult<ParkFitPilotMetricsResult>>> handler = new(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                new GetParkFitPilotMetricsQuery(fromUtc, toUtc),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ParkFitPilotMetricsResult>.Success(metrics));
        AdminParkFitPilotController controller = new(handler.Object);

        IActionResult response = await controller.GetMetricsAsync(
            fromUtc,
            toUtc,
            CancellationToken.None);

        ParkFitPilotMetricsDto body = Assert.IsType<ParkFitPilotMetricsDto>(
            Assert.IsType<OkObjectResult>(response).Value);
        Assert.Equal(1, body.SearchesAbandoned);
        Assert.Equal("Encouraging", body.Health.Signal);
        Assert.Null(typeof(ParkFitPilotMetricsDto).GetProperty("UserId"));
        Assert.Null(typeof(ParkFitPilotMetricsDto).GetProperty("ParkId"));
        handler.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldBeAdminOnlyNoStoreAndRateLimited()
    {
        RouteAttribute route = Assert.IsType<RouteAttribute>(
            typeof(AdminParkFitPilotController).GetCustomAttribute<RouteAttribute>());
        Assert.Equal("admin/park-fit/pilot", route.Template);
        AuthorizeAttribute authorize = Assert.Single(
            typeof(AdminParkFitPilotController).GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.Admin, authorize.Roles);
        Assert.NotNull(typeof(AdminParkFitPilotController)
            .GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
            typeof(AdminParkFitPilotController).GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);
    }
}
