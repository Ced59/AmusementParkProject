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

public sealed class PassportVisitAssessmentsControllerTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task UpsertAsync_ShouldUseTheAuthenticatedOwnerAndMapTheAssessment()
    {
        Mock<ICommandHandler<UpsertVisitParkAssessmentCommand, ApplicationResult<VisitResult>>> upsert =
            new Mock<ICommandHandler<UpsertVisitParkAssessmentCommand, ApplicationResult<VisitResult>>>(MockBehavior.Strict);
        upsert.Setup(handler => handler.HandleAsync(
                It.Is<UpsertVisitParkAssessmentCommand>(command =>
                    command.UserId == "owner-1"
                    && command.VisitId == "visit-1"
                    && command.Value == 4.5d
                    && command.PrivateComment == "Belle journée"
                    && command.ExpectedVersion == 2),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<VisitResult>.Success(CreateResult()));
        PassportVisitAssessmentsController controller = CreateController(upsert.Object);
        controller.ControllerContext = CreateControllerContext();

        IActionResult result = await controller.UpsertAsync(
            "visit-1",
            new UpsertPassportVisitParkAssessmentRequestDto
            {
                Value = 4.5d,
                PrivateComment = "Belle journée",
                ExpectedVersion = 2,
            },
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        PassportVisitDto body = Assert.IsType<PassportVisitDto>(ok.Value);
        Assert.Equal(3, body.Version);
        Assert.Equal(4.5d, body.ParkAssessment?.Value);
        Assert.Equal(2, body.ParkAssessment?.Revision);
        Assert.Null(typeof(PassportVisitParkAssessmentDto).GetProperty("UserId"));
        upsert.VerifyAll();
    }

    [Fact]
    public async Task DeleteAsync_ShouldForwardTheExpectedParentVersion()
    {
        Mock<ICommandHandler<DeleteVisitParkAssessmentCommand, ApplicationResult<VisitResult>>> delete =
            new Mock<ICommandHandler<DeleteVisitParkAssessmentCommand, ApplicationResult<VisitResult>>>(MockBehavior.Strict);
        delete.Setup(handler => handler.HandleAsync(
                new DeleteVisitParkAssessmentCommand("owner-1", "visit-1", 3),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<VisitResult>.Success(CreateResult(includeAssessment: false)));
        PassportVisitAssessmentsController controller = CreateController(deleteHandler: delete.Object);
        controller.ControllerContext = CreateControllerContext();

        IActionResult result = await controller.DeleteAsync(
            "visit-1",
            3,
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Null(Assert.IsType<PassportVisitDto>(ok.Value).ParkAssessment);
        delete.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldExposePrivateNoStorePutAndDeleteRoutes()
    {
        RouteAttribute route = Assert.IsType<RouteAttribute>(
            typeof(PassportVisitAssessmentsController).GetCustomAttribute<RouteAttribute>());
        Assert.Equal("me/passport/visits/{visitId}/assessment", route.Template);
        AuthorizeAttribute authorize = Assert.Single(
            typeof(PassportVisitAssessmentsController).GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.UserModeratorAdmin, authorize.Roles);
        Assert.NotNull(typeof(PassportVisitAssessmentsController)
            .GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
            typeof(PassportVisitAssessmentsController).GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);

        Assert.NotNull(typeof(PassportVisitAssessmentsController)
            .GetMethod(nameof(PassportVisitAssessmentsController.UpsertAsync))
            ?.GetCustomAttribute<HttpPutAttribute>());
        Assert.NotNull(typeof(PassportVisitAssessmentsController)
            .GetMethod(nameof(PassportVisitAssessmentsController.DeleteAsync))
            ?.GetCustomAttribute<HttpDeleteAttribute>());
    }

    private static PassportVisitAssessmentsController CreateController(
        ICommandHandler<UpsertVisitParkAssessmentCommand, ApplicationResult<VisitResult>>? upsertHandler = null,
        ICommandHandler<DeleteVisitParkAssessmentCommand, ApplicationResult<VisitResult>>? deleteHandler = null)
    {
        return new PassportVisitAssessmentsController(
            upsertHandler ?? new Mock<ICommandHandler<UpsertVisitParkAssessmentCommand, ApplicationResult<VisitResult>>>(MockBehavior.Strict).Object,
            deleteHandler ?? new Mock<ICommandHandler<DeleteVisitParkAssessmentCommand, ApplicationResult<VisitResult>>>(MockBehavior.Strict).Object);
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

    private static VisitResult CreateResult(bool includeAssessment = true)
    {
        VisitParkAssessmentResult? resolvedAssessment = !includeAssessment
            ? null
            : new VisitParkAssessmentResult(
                4.5d,
                "Belle journée",
                2,
                NowUtc.AddHours(-1),
                NowUtc);

        return new VisitResult(
            "visit-1",
            "park-1",
            new VisitDateResult(2026, 9, 3, VisitDatePrecision.Day, false),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            VisitStatus.Draft,
            VisitPrivacy.Private,
            null,
            null,
            3,
            NowUtc.AddHours(-2),
            NowUtc,
            null,
            resolvedAssessment);
    }
}
