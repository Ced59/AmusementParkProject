using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
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

public sealed class PassportRideAssessmentsControllerTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task UpsertAsync_ShouldUseTheAuthenticatedOwnerAndMapTheAssessment()
    {
        Mock<ICommandHandler<UpsertRideAssessmentCommand, ApplicationResult<RideOccurrenceResult>>> upsert =
            new Mock<ICommandHandler<UpsertRideAssessmentCommand, ApplicationResult<RideOccurrenceResult>>>(MockBehavior.Strict);
        upsert.Setup(handler => handler.HandleAsync(
                It.Is<UpsertRideAssessmentCommand>(command =>
                    command.UserId == "owner-1"
                    && command.OccurrenceId == "occurrence-1"
                    && command.Value == 4.5d
                    && command.PrivateComment == "Tour mémorable"
                    && command.ExpectedVersion == 2),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<RideOccurrenceResult>.Success(CreateResult()));
        PassportRideAssessmentsController controller = CreateController(upsert.Object);
        controller.ControllerContext = CreateControllerContext();

        IActionResult result = await controller.UpsertAsync(
            "occurrence-1",
            new UpsertPassportRideAssessmentRequestDto
            {
                Value = 4.5d,
                PrivateComment = "Tour mémorable",
                ExpectedVersion = 2,
            },
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        PassportRideOccurrenceDto body = Assert.IsType<PassportRideOccurrenceDto>(ok.Value);
        Assert.Equal(3, body.Version);
        Assert.Equal(4.5d, body.Assessment?.Value);
        Assert.Equal(2, body.Assessment?.Revision);
        Assert.Null(typeof(PassportRideAssessmentDto).GetProperty("UserId"));
        upsert.VerifyAll();
    }

    [Fact]
    public async Task DeleteAsync_ShouldForwardTheExpectedOccurrenceVersion()
    {
        Mock<ICommandHandler<DeleteRideAssessmentCommand, ApplicationResult<RideOccurrenceResult>>> delete =
            new Mock<ICommandHandler<DeleteRideAssessmentCommand, ApplicationResult<RideOccurrenceResult>>>(MockBehavior.Strict);
        delete.Setup(handler => handler.HandleAsync(
                new DeleteRideAssessmentCommand("owner-1", "occurrence-1", 3),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<RideOccurrenceResult>.Success(CreateResult(includeAssessment: false)));
        PassportRideAssessmentsController controller = CreateController(deleteHandler: delete.Object);
        controller.ControllerContext = CreateControllerContext();

        IActionResult result = await controller.DeleteAsync("occurrence-1", 3, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Null(Assert.IsType<PassportRideOccurrenceDto>(ok.Value).Assessment);
        delete.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldExposePrivateNoStorePutAndDeleteRoutes()
    {
        RouteAttribute route = Assert.IsType<RouteAttribute>(
            typeof(PassportRideAssessmentsController).GetCustomAttribute<RouteAttribute>());
        Assert.Equal("me/passport/occurrences/{occurrenceId}/assessment", route.Template);
        AuthorizeAttribute authorize = Assert.Single(
            typeof(PassportRideAssessmentsController).GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.UserModeratorAdmin, authorize.Roles);
        Assert.NotNull(typeof(PassportRideAssessmentsController)
            .GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
            typeof(PassportRideAssessmentsController).GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);

        Assert.NotNull(typeof(PassportRideAssessmentsController)
            .GetMethod(nameof(PassportRideAssessmentsController.UpsertAsync))
            ?.GetCustomAttribute<HttpPutAttribute>());
        Assert.NotNull(typeof(PassportRideAssessmentsController)
            .GetMethod(nameof(PassportRideAssessmentsController.DeleteAsync))
            ?.GetCustomAttribute<HttpDeleteAttribute>());
    }

    private static PassportRideAssessmentsController CreateController(
        ICommandHandler<UpsertRideAssessmentCommand, ApplicationResult<RideOccurrenceResult>>? upsertHandler = null,
        ICommandHandler<DeleteRideAssessmentCommand, ApplicationResult<RideOccurrenceResult>>? deleteHandler = null)
    {
        return new PassportRideAssessmentsController(
            upsertHandler ?? new Mock<ICommandHandler<UpsertRideAssessmentCommand, ApplicationResult<RideOccurrenceResult>>>(MockBehavior.Strict).Object,
            deleteHandler ?? new Mock<ICommandHandler<DeleteRideAssessmentCommand, ApplicationResult<RideOccurrenceResult>>>(MockBehavior.Strict).Object);
    }

    private static ControllerContext CreateControllerContext()
    {
        ClaimsIdentity identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "owner-1"),
                new Claim(ClaimTypes.Role, "USER"),
            },
            "Test");
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity),
            },
        };
    }

    private static RideOccurrenceResult CreateResult(bool includeAssessment = true)
    {
        RideAssessmentResult? assessment = includeAssessment
            ? new RideAssessmentResult(4.5d, "Tour mémorable", 2, NowUtc.AddHours(-1), NowUtc)
            : null;
        return new RideOccurrenceResult(
            "occurrence-1",
            "visit-1",
            "park-1",
            "item-1",
            1024,
            new RideOccurrenceMomentResult(new TimeOnly(10, 30), false),
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
            null,
            true,
            3,
            NowUtc.AddHours(-2),
            NowUtc,
            null,
            assessment);
    }
}
