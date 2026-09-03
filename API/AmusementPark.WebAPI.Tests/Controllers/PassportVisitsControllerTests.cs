using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
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

public sealed class PassportVisitsControllerTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 3, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_ShouldUseTheAuthenticatedOwnerAndIdempotencyHeader()
    {
        Mock<ICommandHandler<CreateVisitCommand, ApplicationResult<CreateVisitResult>>> create =
            new Mock<ICommandHandler<CreateVisitCommand, ApplicationResult<CreateVisitResult>>>(MockBehavior.Strict);
        create.Setup(handler => handler.HandleAsync(
                It.Is<CreateVisitCommand>(command =>
                    command.UserId == "owner-1"
                    && command.ClientOperationId == "request-1"
                    && command.ParkId == "park-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<CreateVisitResult>.Success(
                new CreateVisitResult(CreateResult("visit-1"), false)));
        PassportVisitsController controller = CreateController(create.Object);
        controller.ControllerContext = CreateControllerContext("owner-1");
        controller.Request.Headers[PassportVisitsController.ForwardedPrefixHeaderName] = "/api";

        IActionResult result = await controller.CreateAsync(
            CreateRequest(),
            "request-1",
            CancellationToken.None);

        CreatedResult created = Assert.IsType<CreatedResult>(result);
        Assert.Equal("/api/me/passport/visits/visit-1", created.Location);
        PassportVisitDto body = Assert.IsType<PassportVisitDto>(created.Value);
        Assert.Equal("visit-1", body.Id);
        Assert.Null(typeof(PassportVisitDto).GetProperty("UserId"));
        create.VerifyAll();
    }

    [Fact]
    public async Task CreateAsync_WhenReplayed_ShouldKeepCreatedContractAndExposeReplayHeader()
    {
        Mock<ICommandHandler<CreateVisitCommand, ApplicationResult<CreateVisitResult>>> create =
            new Mock<ICommandHandler<CreateVisitCommand, ApplicationResult<CreateVisitResult>>>(MockBehavior.Strict);
        create.Setup(handler => handler.HandleAsync(
                It.IsAny<CreateVisitCommand>(),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<CreateVisitResult>.Success(
                new CreateVisitResult(CreateResult("visit-1"), true)));
        PassportVisitsController controller = CreateController(create.Object);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.CreateAsync(
            CreateRequest(),
            "request-1",
            CancellationToken.None);

        Assert.IsType<CreatedResult>(result);
        Assert.Equal("true", controller.Response.Headers["Idempotency-Replayed"]);
    }

    [Fact]
    public void BuildVisitLocation_ShouldFallbackToPathBaseAndEscapeTheIdentifier()
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.PathBase = "/backend";

        string result = PassportVisitsController.BuildVisitLocation(
            httpContext.Request,
            "visit 1");

        Assert.Equal("/backend/me/passport/visits/visit%201", result);
    }

    [Fact]
    public async Task ListAsync_WhenCursorIsInvalid_ShouldReturnProblemDetailsWithoutCallingApplication()
    {
        Mock<IQueryHandler<ListUserVisitsQuery, ApplicationResult<VisitPageResult>>> list =
            new Mock<IQueryHandler<ListUserVisitsQuery, ApplicationResult<VisitPageResult>>>(MockBehavior.Strict);
        PassportVisitsController controller = CreateController(listHandler: list.Object);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.ListAsync(
            new PassportVisitListRequestDto { Cursor = "invalid!" },
            CancellationToken.None);

        ObjectResult problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        ProblemDetails body = Assert.IsType<ProblemDetails>(problem.Value);
        Assert.Equal("visit.cursor.invalid", body.Extensions["errorCode"]);
        list.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldForwardOnlyTheAuthenticatedOwner()
    {
        Mock<IQueryHandler<GetVisitQuery, ApplicationResult<VisitResult>>> get =
            new Mock<IQueryHandler<GetVisitQuery, ApplicationResult<VisitResult>>>(MockBehavior.Strict);
        get.Setup(handler => handler.HandleAsync(
                new GetVisitQuery("owner-1", "visit-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<VisitResult>.Success(CreateResult("visit-1")));
        PassportVisitsController controller = CreateController(getHandler: get.Object);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.GetByIdAsync("visit-1", CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("visit-1", Assert.IsType<PassportVisitDto>(ok.Value).Id);
        get.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldExposePrivateNoStoreAdditiveRoutes()
    {
        RouteAttribute route = Assert.IsType<RouteAttribute>(
            typeof(PassportVisitsController).GetCustomAttribute<RouteAttribute>());
        Assert.Equal("me/passport/visits", route.Template);
        AuthorizeAttribute authorize = Assert.Single(
            typeof(PassportVisitsController).GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.UserModeratorAdmin, authorize.Roles);
        Assert.NotNull(typeof(PassportVisitsController)
            .GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
            typeof(PassportVisitsController).GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);
        Assert.Equal(ResponseCacheLocation.None, cache.Location);

        Assert.NotNull(GetAction(nameof(PassportVisitsController.CreateAsync))
            .GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(GetAction(nameof(PassportVisitsController.ListAsync))
            .GetCustomAttribute<HttpGetAttribute>());
        HttpGetAttribute detail = Assert.IsType<HttpGetAttribute>(
            GetAction(nameof(PassportVisitsController.GetByIdAsync))
                .GetCustomAttribute<HttpGetAttribute>());
        Assert.Equal("{visitId}", detail.Template);
        ParameterInfo idempotencyHeader = GetAction(nameof(PassportVisitsController.CreateAsync))
            .GetParameters()
            .Single(parameter => parameter.Name == "idempotencyKey");
        FromHeaderAttribute fromHeader = Assert.IsType<FromHeaderAttribute>(
            idempotencyHeader.GetCustomAttribute<FromHeaderAttribute>());
        Assert.Equal("Idempotency-Key", fromHeader.Name);
        Assert.NotNull(idempotencyHeader.GetCustomAttribute<RequiredAttribute>());
        NullabilityInfo nullability = new NullabilityInfoContext().Create(idempotencyHeader);
        Assert.Equal(NullabilityState.NotNull, nullability.ReadState);
    }

    private static MethodInfo GetAction(string name)
    {
        return typeof(PassportVisitsController).GetMethod(name)
            ?? throw new InvalidOperationException($"Action {name} was not found.");
    }

    private static PassportVisitsController CreateController(
        ICommandHandler<CreateVisitCommand, ApplicationResult<CreateVisitResult>>? createHandler = null,
        IQueryHandler<ListUserVisitsQuery, ApplicationResult<VisitPageResult>>? listHandler = null,
        IQueryHandler<GetVisitQuery, ApplicationResult<VisitResult>>? getHandler = null)
    {
        return new PassportVisitsController(
            createHandler ?? new Mock<ICommandHandler<CreateVisitCommand, ApplicationResult<CreateVisitResult>>>(MockBehavior.Strict).Object,
            listHandler ?? new Mock<IQueryHandler<ListUserVisitsQuery, ApplicationResult<VisitPageResult>>>(MockBehavior.Strict).Object,
            getHandler ?? new Mock<IQueryHandler<GetVisitQuery, ApplicationResult<VisitResult>>>(MockBehavior.Strict).Object);
    }

    private static ControllerContext CreateControllerContext(string userId)
    {
        ClaimsIdentity identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
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

    private static CreatePassportVisitRequestDto CreateRequest()
    {
        return new CreatePassportVisitRequestDto
        {
            ParkId = "park-1",
            Date = new PassportVisitDateDto
            {
                Year = 2026,
                Month = 8,
                Day = 31,
                Precision = PassportVisitDatePrecisionDto.Day,
            },
            TimeZoneId = "Europe/Paris",
            ServiceDayConvention = PassportLocalServiceDayConventionDto.VisitStartLocalDate,
            Title = "Journée d'été",
            PrivateNote = "Note privée",
        };
    }

    private static VisitResult CreateResult(string visitId)
    {
        return new VisitResult(
            visitId,
            "park-1",
            new VisitDateResult(2026, 8, 31, VisitDatePrecision.Day, false),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            VisitStatus.Draft,
            VisitPrivacy.Private,
            "Journée d'été",
            "Note privée",
            1,
            NowUtc,
            NowUtc,
            null);
    }
}
