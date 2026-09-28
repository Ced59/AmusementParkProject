using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
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

public sealed class PassportHistoricalStatisticsControllerTests
{
    [Fact]
    public async Task GetAsync_ShouldUseOwnerAndNeverExposeTechnicalIds()
    {
        Mock<IQueryHandler<
            GetPassportHistoricalStatisticsQuery,
            ApplicationResult<PassportHistoricalStatisticsResult>>> handler = new();
        handler.Setup(item => item.HandleAsync(
                new GetPassportHistoricalStatisticsQuery("owner-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<PassportHistoricalStatisticsResult>.Success(
                CreateResult()));
        PassportHistoricalStatisticsController controller = new(handler.Object)
        {
            ControllerContext = CreateControllerContext("owner-1"),
        };

        IActionResult response = await controller.GetAsync(CancellationToken.None);

        PassportHistoricalStatisticsDto body = Assert.IsType<PassportHistoricalStatisticsDto>(
            Assert.IsType<OkObjectResult>(response).Value);
        Assert.Equal("Parc témoin", Assert.Single(body.ParksAcrossEras).ParkName);
        Assert.Equal("Ancien nom", Assert.Single(body.HistoricalNames).NameAtVisit);
        Assert.Null(typeof(PassportHistoricalParkEraDto).GetProperty("ParkId"));
        Assert.Null(typeof(PassportHistoricalNameDto).GetProperty("ParkItemId"));
        handler.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldRemainPrivateAndNonCacheable()
    {
        Type type = typeof(PassportHistoricalStatisticsController);
        Assert.Equal(
            "me/passport/stats/history",
            type.GetCustomAttribute<RouteAttribute>()?.Template);
        AuthorizeAttribute authorize = Assert.Single(
            type.GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.UserModeratorAdmin, authorize.Roles);
        Assert.NotNull(type.GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        Assert.True(type.GetCustomAttribute<ResponseCacheAttribute>()?.NoStore);
    }

    private static PassportHistoricalStatisticsResult CreateResult()
    {
        return new PassportHistoricalStatisticsResult(
            2,
            2001,
            1,
            1,
            1d,
            1,
            new[] { new PassportHistoricalParkEraResult("Parc témoin", 2001, 2026, 2, 2) },
            Array.Empty<PassportHistoricalDisappearedAttractionResult>(),
            Array.Empty<PassportHistoricalTransformationResult>(),
            new[]
            {
                new PassportHistoricalNameResult(
                    "Parc témoin",
                    "Ancien nom",
                    "Nouveau nom",
                    2001,
                    2001,
                    1),
            },
            Array.Empty<PassportHistoricalCategoryResult>());
    }

    private static ControllerContext CreateControllerContext(string userId)
    {
        ClaimsIdentity identity = new(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
            "Test");
        DefaultHttpContext context = new()
        {
            User = new ClaimsPrincipal(identity),
        };
        return new ControllerContext { HttpContext = context };
    }
}
