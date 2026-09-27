using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.History;
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

public sealed class PassportHistoricalRideTargetsControllerTests
{
    [Fact]
    public async Task ListAsync_ShouldForwardPrivateFiltersAndMapHistoricalContext()
    {
        Mock<IQueryHandler<
            ListVisitHistoricalRideTargetsQuery,
            ApplicationResult<VisitHistoricalRideTargetPageResult>>> handler = new(
                MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<ListVisitHistoricalRideTargetsQuery>(query =>
                    query.UserId == "owner-1"
                    && query.VisitId == "visit-1"
                    && query.Search == "cyclone"
                    && query.Scope == VisitHistoricalTargetScope.PossiblyOpen
                    && query.ZoneId == "zone-1"
                    && query.Page == 2
                    && query.PageSize == 12),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<VisitHistoricalRideTargetPageResult>.Success(
                new VisitHistoricalRideTargetPageResult(
                    new[]
                    {
                        new VisitRideTargetEvaluationResult(
                            "ride-1",
                            "Le Cyclone",
                            "Attraction",
                            HistoricalOperationalState.PossiblyOpen,
                            HistoricalConsistency.Unverified,
                            true,
                            "image-1",
                            "zone-1",
                            "Removed",
                            null,
                            null),
                    },
                    2,
                    12,
                    13,
                    2,
                    5,
                    3,
                    18,
                    HistoricalCoverageStatus.Substantial,
                    72,
                    "history-v2")));
        PassportHistoricalRideTargetsController controller = new(handler.Object)
        {
            ControllerContext = CreateControllerContext(),
        };

        IActionResult result = await controller.ListAsync(
            "visit-1",
            "cyclone",
            "possiblyopen",
            "zone-1",
            2,
            12,
            CancellationToken.None);

        PassportHistoricalRideTargetPageDto page = Assert.IsType<PassportHistoricalRideTargetPageDto>(
            Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(72, page.CoveragePercent);
        Assert.Equal("Substantial", page.CoverageStatus);
        Assert.Equal("PossiblyOpen", Assert.Single(page.Items).OperationalState);
        handler.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldRemainPrivateNonCacheableAndReadOnly()
    {
        RouteAttribute? route = typeof(PassportHistoricalRideTargetsController)
            .GetCustomAttribute<RouteAttribute>();
        Assert.Equal("me/passport/visits/{visitId}/historical-ride-targets", route?.Template);
        AuthorizeAttribute authorize = Assert.Single(
            typeof(PassportHistoricalRideTargetsController).GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.UserModeratorAdmin, authorize.Roles);
        Assert.NotNull(typeof(PassportHistoricalRideTargetsController)
            .GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        Assert.True(typeof(PassportHistoricalRideTargetsController)
            .GetCustomAttribute<ResponseCacheAttribute>()?.NoStore);
        Assert.NotNull(typeof(PassportHistoricalRideTargetsController)
            .GetMethod(nameof(PassportHistoricalRideTargetsController.ListAsync))
            ?.GetCustomAttribute<HttpGetAttribute>());
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
