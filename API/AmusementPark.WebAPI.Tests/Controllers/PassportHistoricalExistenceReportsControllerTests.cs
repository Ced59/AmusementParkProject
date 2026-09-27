using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.HistoricalExistenceReports.Commands;
using AmusementPark.Application.Features.HistoricalExistenceReports.Queries;
using AmusementPark.Application.Features.HistoricalExistenceReports.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.History;
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

public sealed class PassportHistoricalExistenceReportsControllerTests
{
    [Fact]
    public async Task SubmitAsync_ShouldBindTheAuthenticatedOwnerAndReturnCreatedReport()
    {
        Mock<IQueryHandler<
            ListOwnedHistoricalExistenceReportsQuery,
            ApplicationResult<IReadOnlyCollection<HistoricalExistenceReportResult>>>> query =
            new(MockBehavior.Strict);
        Mock<ICommandHandler<
            SubmitHistoricalExistenceReportCommand,
            ApplicationResult<HistoricalExistenceReportResult>>> command =
            new(MockBehavior.Strict);
        HistoricalExistenceReportResult report = CreateResult();
        command.Setup(handler => handler.HandleAsync(
                It.Is<SubmitHistoricalExistenceReportCommand>(request =>
                    request.UserId == "owner-1"
                    && request.VisitId == "visit-1"
                    && request.ClaimedName == "Ancien Cyclone"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<HistoricalExistenceReportResult>.Success(report));
        PassportHistoricalExistenceReportsController controller = new(
            query.Object,
            command.Object)
        {
            ControllerContext = CreateControllerContext("owner-1"),
        };

        IActionResult result = await controller.SubmitAsync(
            "visit-1",
            new SubmitHistoricalExistenceReportRequestDto
            {
                ClaimedName = "Ancien Cyclone",
            },
            CancellationToken.None);

        ObjectResult created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        HistoricalExistenceReportDto dto =
            Assert.IsType<HistoricalExistenceReportDto>(created.Value);
        Assert.Equal("Parc témoin", dto.ParkName);
        Assert.Equal("Pending", dto.Status);
        command.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldRemainPrivateNonCacheableAndRateLimited()
    {
        Type type = typeof(PassportHistoricalExistenceReportsController);
        Assert.Equal(
            "me/passport/visits/{visitId}/historical-existence-reports",
            type.GetCustomAttribute<RouteAttribute>()?.Template);
        AuthorizeAttribute authorize = Assert.Single(
            type.GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.UserModeratorAdmin, authorize.Roles);
        Assert.NotNull(type.GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        Assert.True(type.GetCustomAttribute<ResponseCacheAttribute>()?.NoStore);
        Assert.Equal(
            RateLimitPolicyNames.HistoricalExistenceReports,
            type.GetMethod(nameof(PassportHistoricalExistenceReportsController.SubmitAsync))
                ?.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);
    }

    private static HistoricalExistenceReportResult CreateResult()
    {
        return new HistoricalExistenceReportResult(
            "report-1",
            "park-1",
            "Parc témoin",
            VisitDate.ForYear(1998),
            "Ancien Cyclone",
            null,
            null,
            null,
            HistoricalExistenceReportStatus.Pending,
            new DateTime(2026, 9, 27, 8, 0, 0, DateTimeKind.Utc),
            null,
            null,
            0);
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
