using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.PassportBeta;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class AdminPassportBetaControllerTests
{
    [Fact]
    public async Task GetMetricsAsync_ShouldReturnOnlyAggregatedCohortData()
    {
        DateTime fromUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime toUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        Mock<IQueryHandler<
            GetPassportBetaMetricsQuery,
            ApplicationResult<PassportBetaMetricsResult>>> handler =
            new Mock<IQueryHandler<
                GetPassportBetaMetricsQuery,
                ApplicationResult<PassportBetaMetricsResult>>>(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                new GetPassportBetaMetricsQuery(fromUtc, toUtc),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<PassportBetaMetricsResult>.Success(
                new PassportBetaMetricsResult(
                    toUtc,
                    fromUtc,
                    toUtc,
                    8,
                    6,
                    5,
                    3,
                    60m,
                    PassportBetaRepeatUsageSignal.Candidate,
                    true,
                    new[]
                    {
                        new PassportBetaDailyMetrics("2026-09-01", 2, 1, 1),
                    })));
        AdminPassportBetaController controller = new AdminPassportBetaController(
            handler.Object);

        IActionResult response = await controller.GetMetricsAsync(
            fromUtc,
            toUtc,
            CancellationToken.None);

        PassportBetaMetricsDto body = Assert.IsType<PassportBetaMetricsDto>(
            Assert.IsType<OkObjectResult>(response).Value);
        Assert.Equal(3, body.UsersWithSecondCompletedVisit);
        Assert.Equal("Candidate", body.RepeatUsageSignal);
        Assert.True(body.RequiresQualitativeValidation);
        Assert.Equal(1, Assert.Single(body.Daily).SecondVisits);
        Assert.Null(typeof(PassportBetaMetricsDto).GetProperty("UserId"));
        Assert.Null(typeof(PassportBetaDailyMetricsDto).GetProperty("UserId"));
        handler.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldBeAdminOnlyAndNoStore()
    {
        RouteAttribute route = Assert.IsType<RouteAttribute>(
            typeof(AdminPassportBetaController).GetCustomAttribute<RouteAttribute>());
        Assert.Equal("admin/passport-beta", route.Template);
        AuthorizeAttribute authorize = Assert.Single(
            typeof(AdminPassportBetaController).GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.Admin, authorize.Roles);
        Assert.NotNull(typeof(AdminPassportBetaController)
            .GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
            typeof(AdminPassportBetaController).GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);
        HttpGetAttribute get = Assert.IsType<HttpGetAttribute>(
            typeof(AdminPassportBetaController)
                .GetMethod(nameof(AdminPassportBetaController.GetMetricsAsync))
                ?.GetCustomAttribute<HttpGetAttribute>());
        Assert.Equal("metrics", get.Template);
    }
}
