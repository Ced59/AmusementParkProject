using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Queries;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.LiveData;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class AdminLiveForecastBacktestsControllerTests
{
    [Fact]
    public void Controller_ShouldBeAdminOnlyNoStoreAndRateLimited()
    {
        Type type = typeof(AdminLiveForecastBacktestsController);

        Assert.Equal(
            "admin/live/forecast-backtests",
            type.GetCustomAttribute<RouteAttribute>()?.Template);
        AuthorizeAttribute authorize = Assert.Single(
            type.GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.Admin, authorize.Roles);
        Assert.NotNull(type.GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        Assert.True(type.GetCustomAttribute<ResponseCacheAttribute>()?.NoStore);
        MethodInfo get = type.GetMethod(nameof(AdminLiveForecastBacktestsController.GetAsync))!;
        Assert.Equal(
            RateLimitPolicyNames.LiveDataAdministration,
            get.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnBusinessNamesAndScientificVerdict()
    {
        DateTime fromUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime toUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        LiveWaitForecastBacktestPolicy policy = new LiveWaitForecastBacktestPolicy();
        LiveWaitForecastBacktestReport report = new LiveWaitForecastBacktestReport(
            fromUtc,
            toUtc,
            "Europe/Paris",
            LiveWaitForecastBacktestVerdict.InsufficientData,
            new[] { LiveWaitForecastBacktestReason.InsufficientEvaluationPoints },
            10,
            2,
            0,
            0,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            policy);
        Mock<IQueryHandler<
            GetAdminLiveWaitForecastBacktestQuery,
            ApplicationResult<LiveWaitForecastBacktestResult>>> handler = new(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<GetAdminLiveWaitForecastBacktestQuery>(query =>
                    query.ParkItemId == "item-1"
                    && query.From == new DateTimeOffset(fromUtc)
                    && query.To == new DateTimeOffset(toUtc)),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<LiveWaitForecastBacktestResult>.Success(
                new LiveWaitForecastBacktestResult(
                    "Taron",
                    "Phantasialand",
                    report,
                    toUtc)));
        AdminLiveForecastBacktestsController controller = new(handler.Object);

        IActionResult action = await controller.GetAsync(
            "item-1",
            new DateTimeOffset(fromUtc),
            new DateTimeOffset(toUtc),
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(action);
        LiveWaitForecastBacktestDto response =
            Assert.IsType<LiveWaitForecastBacktestDto>(ok.Value);
        Assert.Equal("Taron", response.TargetDisplayName);
        Assert.Equal("Phantasialand", response.ParkDisplayName);
        Assert.Equal("InsufficientData", response.Verdict);
        Assert.Equal(
            "InsufficientEvaluationPoints",
            Assert.Single(response.Reasons));
        Assert.Equal(28, response.Policy.MinimumBaselineTrainingDays);
        Assert.Equal(8, response.Policy.MinimumCandidateTrainingDays);
        Assert.Equal(3d, response.Policy.MinimumDriftIncreaseMinutes);
        Assert.Equal(LiveWaitForecastBacktestPolicy.BaselineMethod, response.BaselineMethod);
        Assert.Equal(LiveWaitForecastBacktestPolicy.CandidateMethod, response.CandidateMethod);
        Assert.Equal(LiveWaitForecastBacktestPolicy.IntervalMethod, response.IntervalMethod);
        handler.VerifyAll();
    }
}
