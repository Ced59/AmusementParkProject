using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;
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

public sealed class PassportScopeStatisticsControllerTests
{
    [Fact]
    public async Task GetGlobalStatisticsAsync_ShouldUseAuthenticatedOwnerAndMapNames()
    {
        Mock<IQueryHandler<
            GetPassportGlobalStatisticsQuery,
            ApplicationResult<PassportGlobalStatisticsResult>>> globalHandler = CreateGlobalHandler();
        globalHandler.Setup(value => value.HandleAsync(
                new GetPassportGlobalStatisticsQuery("owner-1", 2025, "park-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<PassportGlobalStatisticsResult>.Success(
                new PassportGlobalStatisticsResult(
                    2025,
                    "park-1",
                    new[] { 2025 },
                    new[] { new PassportGlobalFilterParkResult("park-1", "Parc test") },
                    1,
                    CreateSummary(),
                    new[] { new PassportGlobalYearActivityResult(2025, 1, 0) },
                    new[] { new PassportGlobalParkActivityResult("park-1", "Parc test", 1, 0) },
                    Array.Empty<PassportGlobalItemActivityResult>(),
                    Array.Empty<PassportGlobalRatingEvolutionResult>())));
        PassportScopeStatisticsController controller = CreateController(
            CreateParkHandler(),
            CreateYearHandler(),
            authenticated: true,
            globalHandler: globalHandler);

        IActionResult response = await controller.GetGlobalStatisticsAsync(2025, "park-1");

        PassportGlobalStatisticsDto body = Assert.IsType<PassportGlobalStatisticsDto>(
            Assert.IsType<OkObjectResult>(response).Value);
        Assert.Equal("Parc test", Assert.Single(body.AvailableParks).ParkName);
        Assert.Equal("Parc test", Assert.Single(body.TopParks).ParkName);
        Assert.Null(typeof(PassportGlobalStatisticsDto).GetProperty("UserId"));
        globalHandler.VerifyAll();
    }

    [Fact]
    public async Task GetParkStatisticsAsync_ShouldUseAuthenticatedOwnerAndMapEvidence()
    {
        Mock<IQueryHandler<
            GetPassportParkStatisticsQuery,
            ApplicationResult<PassportParkStatisticsResult>>> parkHandler =
            new Mock<IQueryHandler<
                GetPassportParkStatisticsQuery,
                ApplicationResult<PassportParkStatisticsResult>>>(MockBehavior.Strict);
        Mock<IQueryHandler<
            GetPassportYearStatisticsQuery,
            ApplicationResult<PassportYearStatisticsResult>>> yearHandler = CreateYearHandler();
        parkHandler.Setup(value => value.HandleAsync(
                new GetPassportParkStatisticsQuery("owner-1", "park-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<PassportParkStatisticsResult>.Success(
                new PassportParkStatisticsResult(
                    "park-1",
                    CreateSummary(),
                    4.5d,
                    0.5d,
                    new[]
                    {
                        new PassportParkAssessmentPointResult(
                            "visit-1",
                            Date(2025),
                            4d),
                    },
                    Array.Empty<PassportYearBreakdownResult>(),
                    new[]
                    {
                        new PassportCurrentItemRatingResult(
                            "item-1",
                            5d,
                            "Attraction test"),
                    },
                    Array.Empty<PassportHistoricalItemRatingResult>(),
                    "Parc test")));
        PassportScopeStatisticsController controller = CreateController(
            parkHandler,
            yearHandler,
            authenticated: true);

        IActionResult response = await controller.GetParkStatisticsAsync("park-1");

        PassportParkStatisticsDto body = Assert.IsType<PassportParkStatisticsDto>(
            Assert.IsType<OkObjectResult>(response).Value);
        Assert.Equal("park-1", body.ParkId);
        Assert.Equal("Parc test", body.ParkName);
        Assert.Equal("Attraction test", Assert.Single(body.CurrentTopItems).ParkItemName);
        Assert.Equal(1, body.Summary.VisitCount);
        Assert.Equal(4d, Assert.Single(body.AssessmentTimeline).Rating);
        Assert.Null(typeof(PassportParkStatisticsDto).GetProperty("UserId"));
        parkHandler.VerifyAll();
        yearHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetYearStatisticsAsync_ShouldUseAuthenticatedOwnerAndMapBreakdown()
    {
        Mock<IQueryHandler<
            GetPassportParkStatisticsQuery,
            ApplicationResult<PassportParkStatisticsResult>>> parkHandler = CreateParkHandler();
        Mock<IQueryHandler<
            GetPassportYearStatisticsQuery,
            ApplicationResult<PassportYearStatisticsResult>>> yearHandler = CreateYearHandler();
        yearHandler.Setup(value => value.HandleAsync(
                new GetPassportYearStatisticsQuery("owner-1", 2025),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<PassportYearStatisticsResult>.Success(
                new PassportYearStatisticsResult(
                    2025,
                    1,
                    CreateSummary(),
                    new[]
                    {
                        new PassportParkBreakdownResult(
                            "park-1",
                            CreateSummary(),
                            "Parc test"),
                    })));
        PassportScopeStatisticsController controller = CreateController(
            parkHandler,
            yearHandler,
            authenticated: true);

        IActionResult response = await controller.GetYearStatisticsAsync(2025);

        PassportYearStatisticsDto body = Assert.IsType<PassportYearStatisticsDto>(
            Assert.IsType<OkObjectResult>(response).Value);
        Assert.Equal(2025, body.Year);
        Assert.Equal("park-1", Assert.Single(body.ByPark).ParkId);
        Assert.Equal("Parc test", Assert.Single(body.ByPark).ParkName);
        parkHandler.VerifyNoOtherCalls();
        yearHandler.VerifyAll();
    }

    [Fact]
    public async Task GetParkStatisticsAsync_WithoutAuthentication_ShouldNotReadStatistics()
    {
        Mock<IQueryHandler<
            GetPassportParkStatisticsQuery,
            ApplicationResult<PassportParkStatisticsResult>>> parkHandler = CreateParkHandler();
        Mock<IQueryHandler<
            GetPassportYearStatisticsQuery,
            ApplicationResult<PassportYearStatisticsResult>>> yearHandler = CreateYearHandler();
        PassportScopeStatisticsController controller = CreateController(
            parkHandler,
            yearHandler,
            authenticated: false);

        IActionResult response = await controller.GetParkStatisticsAsync("park-1");

        ObjectResult unauthorized = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
        parkHandler.VerifyNoOtherCalls();
        yearHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public void Controller_ShouldExposePrivateNoStoreParkAndYearRoutes()
    {
        RouteAttribute route = Assert.IsType<RouteAttribute>(
            typeof(PassportScopeStatisticsController).GetCustomAttribute<RouteAttribute>());
        Assert.Equal("me/passport", route.Template);
        AuthorizeAttribute authorize = Assert.Single(
            typeof(PassportScopeStatisticsController)
                .GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.UserModeratorAdmin, authorize.Roles);
        Assert.NotNull(typeof(PassportScopeStatisticsController)
            .GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
            typeof(PassportScopeStatisticsController)
                .GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);
        Assert.Equal(
            "stats",
            GetRoute(nameof(PassportScopeStatisticsController.GetGlobalStatisticsAsync)));
        Assert.Equal(
            "parks/{parkId}/stats",
            GetRoute(nameof(PassportScopeStatisticsController.GetParkStatisticsAsync)));
        Assert.Equal(
            "years/{year:int}/stats",
            GetRoute(nameof(PassportScopeStatisticsController.GetYearStatisticsAsync)));
    }

    private static string? GetRoute(string methodName)
    {
        return typeof(PassportScopeStatisticsController)
            .GetMethod(methodName)
            ?.GetCustomAttribute<HttpGetAttribute>()
            ?.Template;
    }

    private static PassportStatisticsSummaryResult CreateSummary()
    {
        return new PassportStatisticsSummaryResult(
            1,
            0,
            new PassportRatingCoverageResult(1, 1, 1d),
            new PassportRatingDistributionResult(1, 4d, 4d, 4d, 4d, 0d),
            new PassportVisitExperienceResult("visit-1", "park-1", Date(2025)),
            new PassportVisitExperienceResult("visit-1", "park-1", Date(2025)),
            new PassportRideOutcomeStatisticsResult(0, 0, 0, 0, 0, 0),
            new PassportRatingCoverageResult(0, 0, 0d),
            null,
            0,
            0,
            Array.Empty<PassportCategoryCoverageResult>());
    }

    private static VisitDateResult Date(int year)
    {
        return new VisitDateResult(year, null, null, VisitDatePrecision.Year, false);
    }

    private static PassportScopeStatisticsController CreateController(
        Mock<IQueryHandler<
            GetPassportParkStatisticsQuery,
            ApplicationResult<PassportParkStatisticsResult>>> parkHandler,
        Mock<IQueryHandler<
            GetPassportYearStatisticsQuery,
            ApplicationResult<PassportYearStatisticsResult>>> yearHandler,
        bool authenticated,
        Mock<IQueryHandler<
            GetPassportGlobalStatisticsQuery,
            ApplicationResult<PassportGlobalStatisticsResult>>>? globalHandler = null)
    {
        ClaimsIdentity identity = authenticated
            ? new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "owner-1"),
                    new Claim(ClaimTypes.Role, "USER"),
                },
                "Test")
            : new ClaimsIdentity();
        Mock<IQueryHandler<
            GetPassportGlobalStatisticsQuery,
            ApplicationResult<PassportGlobalStatisticsResult>>> effectiveGlobalHandler =
            globalHandler ?? CreateGlobalHandler();
        return new PassportScopeStatisticsController(
            effectiveGlobalHandler.Object,
            parkHandler.Object,
            yearHandler.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity),
                },
            },
        };
    }

    private static Mock<IQueryHandler<
        GetPassportGlobalStatisticsQuery,
        ApplicationResult<PassportGlobalStatisticsResult>>> CreateGlobalHandler()
    {
        return new Mock<IQueryHandler<
            GetPassportGlobalStatisticsQuery,
            ApplicationResult<PassportGlobalStatisticsResult>>>(MockBehavior.Strict);
    }

    private static Mock<IQueryHandler<
        GetPassportParkStatisticsQuery,
        ApplicationResult<PassportParkStatisticsResult>>> CreateParkHandler()
    {
        return new Mock<IQueryHandler<
            GetPassportParkStatisticsQuery,
            ApplicationResult<PassportParkStatisticsResult>>>(MockBehavior.Strict);
    }

    private static Mock<IQueryHandler<
        GetPassportYearStatisticsQuery,
        ApplicationResult<PassportYearStatisticsResult>>> CreateYearHandler()
    {
        return new Mock<IQueryHandler<
            GetPassportYearStatisticsQuery,
            ApplicationResult<PassportYearStatisticsResult>>>(MockBehavior.Strict);
    }
}
