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

public sealed class PassportStatisticsControllerTests
{
    [Fact]
    public async Task GetItemStatisticsAsync_ShouldUseTheAuthenticatedOwnerAndMapEvidence()
    {
        Mock<IQueryHandler<
            GetPassportItemStatisticsQuery,
            ApplicationResult<PassportItemStatisticsResult>>> handler =
            new Mock<IQueryHandler<
                GetPassportItemStatisticsQuery,
                ApplicationResult<PassportItemStatisticsResult>>>(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                new GetPassportItemStatisticsQuery("owner-1", "item-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<PassportItemStatisticsResult>.Success(
                CreateResult()));
        PassportStatisticsController controller = new PassportStatisticsController(
            handler.Object)
        {
            ControllerContext = CreateControllerContext(authenticated: true),
        };

        IActionResult response = await controller.GetItemStatisticsAsync(
            "item-1",
            CancellationToken.None);

        PassportItemStatisticsDto body = Assert.IsType<PassportItemStatisticsDto>(
            Assert.IsType<OkObjectResult>(response).Value);
        Assert.Equal("item-1", body.ParkItemId);
        Assert.Equal(3, body.RideCount);
        Assert.Equal(2, body.VisitCount);
        Assert.Equal(2, body.RatingCoverage.RatedRideCount);
        Assert.Equal(3, body.RatingCoverage.TotalRideCount);
        Assert.Equal(2d / 3d, body.RatingCoverage.Rate, 12);
        Assert.Equal(PassportVisitDatePrecisionDto.Year, body.FirstExperience?.Date.Precision);
        Assert.Null(body.FirstExperience?.Date.Month);
        Assert.Equal(3.5d, body.HistoricalRatings?.Average);
        Assert.Equal(0.5d, body.HistoricalRatings?.PopulationStandardDeviation);
        Assert.Equal(4.5d, body.CurrentGlobalRating);
        Assert.Equal(1d, body.CurrentGlobalMinusHistoricalAverage);
        Assert.Equal("visit-1", Assert.Single(body.ByVisit).VisitId);
        Assert.Equal(2024, Assert.Single(body.ByYear).Year);
        Assert.Equal("occ-1", Assert.Single(body.RatingTimeline).RideOccurrenceId);
        Assert.Equal(PassportRatingTrendKindDto.Stable, body.Trend?.Kind);
        Assert.Null(typeof(PassportItemStatisticsDto).GetProperty("UserId"));
        handler.VerifyAll();
    }

    [Fact]
    public async Task GetItemStatisticsAsync_WithoutAuthentication_ShouldNotCallTheHandler()
    {
        Mock<IQueryHandler<
            GetPassportItemStatisticsQuery,
            ApplicationResult<PassportItemStatisticsResult>>> handler =
            new Mock<IQueryHandler<
                GetPassportItemStatisticsQuery,
                ApplicationResult<PassportItemStatisticsResult>>>(MockBehavior.Strict);
        PassportStatisticsController controller = new PassportStatisticsController(
            handler.Object)
        {
            ControllerContext = CreateControllerContext(authenticated: false),
        };

        IActionResult response = await controller.GetItemStatisticsAsync("item-1");

        ObjectResult unauthorized = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
        handler.VerifyNoOtherCalls();
    }

    [Fact]
    public void Controller_ShouldExposeAPrivateNoStoreItemStatisticsRoute()
    {
        RouteAttribute route = Assert.IsType<RouteAttribute>(
            typeof(PassportStatisticsController).GetCustomAttribute<RouteAttribute>());
        Assert.Equal("me/passport", route.Template);
        AuthorizeAttribute authorize = Assert.Single(
            typeof(PassportStatisticsController).GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.UserModeratorAdmin, authorize.Roles);
        Assert.NotNull(typeof(PassportStatisticsController)
            .GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
            typeof(PassportStatisticsController).GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);
        HttpGetAttribute get = Assert.IsType<HttpGetAttribute>(
            typeof(PassportStatisticsController)
                .GetMethod(nameof(PassportStatisticsController.GetItemStatisticsAsync))
                ?.GetCustomAttribute<HttpGetAttribute>());
        Assert.Equal("items/{parkItemId}/stats", get.Template);
    }

    private static PassportItemStatisticsResult CreateResult()
    {
        return new PassportItemStatisticsResult(
            "item-1",
            3,
            2,
            new PassportItemRatingCoverageResult(2, 3, 2d / 3d),
            new PassportItemExperienceResult(
                "visit-1",
                new VisitDateResult(2024, null, null, VisitDatePrecision.Year, true)),
            new PassportItemExperienceResult(
                "visit-2",
                new VisitDateResult(2025, 6, 1, VisitDatePrecision.Day, false)),
            new PassportRatingDistributionResult(2, 3.5d, 3.5d, 3d, 4d, 0.5d),
            4.5d,
            1d,
            new[]
            {
                new PassportItemVisitStatisticsResult(
                    "visit-1",
                    new VisitDateResult(2024, null, null, VisitDatePrecision.Year, true),
                    1,
                    new PassportItemRatingCoverageResult(1, 1, 1d),
                    new PassportRatingDistributionResult(1, 3d, 3d, 3d, 3d, 0d)),
            },
            new[]
            {
                new PassportItemYearStatisticsResult(
                    2024,
                    1,
                    1,
                    new PassportItemRatingCoverageResult(1, 1, 1d),
                    new PassportRatingDistributionResult(1, 3d, 3d, 3d, 3d, 0d)),
            },
            new[]
            {
                new PassportItemRatingPointResult(
                    "occ-1",
                    "visit-1",
                    new VisitDateResult(2024, null, null, VisitDatePrecision.Year, true),
                    1024,
                    3d),
            },
            new PassportRatingTrendResult(
                PassportRatingTrendKind.Stable,
                1,
                1,
                3d,
                3d,
                0d));
    }

    private static ControllerContext CreateControllerContext(bool authenticated)
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
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity),
            },
        };
    }
}
