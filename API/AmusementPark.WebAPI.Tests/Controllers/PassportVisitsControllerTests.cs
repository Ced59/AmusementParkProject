using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
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
    public async Task ListAsync_ShouldOmitThePrivateAssessmentPayload()
    {
        VisitResult assessedVisit = CreateResult("visit-1") with
        {
            ParkName = "Parc test",
            PrivateNote = "Souvenir privé",
            ParkAssessment = new VisitParkAssessmentResult(
                4.5,
                new string('x', 4000),
                1,
                NowUtc,
                NowUtc),
        };
        Mock<IQueryHandler<ListUserVisitsQuery, ApplicationResult<VisitPageResult>>> list =
            new Mock<IQueryHandler<ListUserVisitsQuery, ApplicationResult<VisitPageResult>>>(MockBehavior.Strict);
        list.Setup(handler => handler.HandleAsync(
                It.Is<ListUserVisitsQuery>(query => query.UserId == "owner-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<VisitPageResult>.Success(
                new VisitPageResult(new[] { assessedVisit }, null)));
        PassportVisitsController controller = CreateController(listHandler: list.Object);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.ListAsync(
            new PassportVisitListRequestDto(),
            CancellationToken.None);

        PassportVisitPageDto page = Assert.IsType<PassportVisitPageDto>(
            Assert.IsType<OkObjectResult>(result).Value);
        PassportVisitDto item = Assert.Single(page.Items);
        Assert.Equal("visit-1", item.Id);
        Assert.Equal("Parc test", item.ParkName);
        Assert.Null(item.PrivateNote);
        Assert.True(item.HasPrivateNote);
        Assert.Null(item.ParkAssessment);
        list.VerifyAll();
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
    public async Task UpdateAsync_ShouldUseTheAuthenticatedOwnerAndReturnTheNewVersion()
    {
        Mock<ICommandHandler<UpdateVisitMetadataCommand, ApplicationResult<VisitResult>>> update =
            new Mock<ICommandHandler<UpdateVisitMetadataCommand, ApplicationResult<VisitResult>>>(MockBehavior.Strict);
        update.Setup(handler => handler.HandleAsync(
                It.Is<UpdateVisitMetadataCommand>(command =>
                    command.UserId == "owner-1"
                    && command.VisitId == "visit-1"
                    && command.ExpectedVersion == 1),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<VisitResult>.Success(CreateResult("visit-1") with { Version = 2 }));
        PassportVisitsController controller = CreateController(updateHandler: update.Object);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.UpdateAsync(
            "visit-1",
            new UpdatePassportVisitRequestDto
            {
                Date = CreateRequest().Date,
                TimeZoneId = "Europe/Paris",
                ExpectedVersion = 1,
            },
            CancellationToken.None);

        PassportVisitDto body = Assert.IsType<PassportVisitDto>(
            Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(2, body.Version);
        update.VerifyAll();
    }

    [Fact]
    public async Task CompleteAsync_ShouldUseTheAuthenticatedOwnerAndVersionFence()
    {
        Mock<ICommandHandler<CompleteVisitCommand, ApplicationResult<VisitResult>>> complete =
            new Mock<ICommandHandler<CompleteVisitCommand, ApplicationResult<VisitResult>>>(MockBehavior.Strict);
        complete.Setup(handler => handler.HandleAsync(
                new CompleteVisitCommand("owner-1", "visit-1", 3),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<VisitResult>.Success(CreateResult("visit-1") with { Version = 4 }));
        PassportVisitsController controller = CreateController(completeHandler: complete.Object);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.CompleteAsync(
            "visit-1",
            new MutatePassportVisitStatusRequestDto { ExpectedVersion = 3 },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        complete.VerifyAll();
    }

    [Fact]
    public async Task GetDeletionPreviewAsync_ShouldUseTheAuthenticatedOwner()
    {
        Mock<IQueryHandler<GetVisitDeletionPreviewQuery, ApplicationResult<VisitDeletionPreview>>> handler =
            new Mock<IQueryHandler<GetVisitDeletionPreviewQuery, ApplicationResult<VisitDeletionPreview>>>(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<GetVisitDeletionPreviewQuery>(query =>
                    query.UserId == "owner-1" && query.VisitId == "visit-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<VisitDeletionPreview>.Success(
                new VisitDeletionPreview("visit-1", 3, 4, 2, 7)));
        PassportVisitsController controller = CreateController(deletionPreviewHandler: handler.Object);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.GetDeletionPreviewAsync(
            "visit-1",
            CancellationToken.None);

        PassportVisitDeletionPreviewDto body = Assert.IsType<PassportVisitDeletionPreviewDto>(
            Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(4, body.OccurrenceCount);
        Assert.Equal(2, body.AssessmentCount);
        handler.VerifyAll();
    }

    [Fact]
    public async Task DeleteAsync_ShouldForwardConfirmationAndIdempotencyKey()
    {
        Mock<ICommandHandler<DeleteVisitCommand, ApplicationResult<VisitDeletionReceipt>>> handler =
            new Mock<ICommandHandler<DeleteVisitCommand, ApplicationResult<VisitDeletionReceipt>>>(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<DeleteVisitCommand>(command =>
                    command.UserId == "owner-1"
                    && command.VisitId == "visit-1"
                    && command.ExpectedVersion == 3
                    && command.ConfirmedOccurrenceCount == 4
                    && command.ConfirmedAssessmentCount == 2
                    && command.ClientOperationId == "delete-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<VisitDeletionReceipt>.Success(
                new VisitDeletionReceipt(
                    "visit-1",
                    NowUtc,
                    NowUtc.AddDays(7),
                    false)));
        PassportVisitsController controller = CreateController(deleteHandler: handler.Object);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.DeleteAsync(
            "visit-1",
            new DeletePassportVisitRequestDto
            {
                ExpectedVersion = 3,
                ConfirmedOccurrenceCount = 4,
                ConfirmedAssessmentCount = 2,
            },
            "delete-1",
            CancellationToken.None);

        PassportVisitDeletionReceiptDto body = Assert.IsType<PassportVisitDeletionReceiptDto>(
            Assert.IsType<AcceptedResult>(result).Value);
        Assert.Equal("visit-1", body.VisitId);
        handler.VerifyAll();
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
        Assert.Equal("{visitId}", Assert.IsType<HttpPatchAttribute>(
            GetAction(nameof(PassportVisitsController.UpdateAsync)).GetCustomAttribute<HttpPatchAttribute>()).Template);
        Assert.Equal("{visitId}/complete", Assert.IsType<HttpPostAttribute>(
            GetAction(nameof(PassportVisitsController.CompleteAsync)).GetCustomAttribute<HttpPostAttribute>()).Template);
        Assert.Equal("{visitId}/reopen", Assert.IsType<HttpPostAttribute>(
            GetAction(nameof(PassportVisitsController.ReopenAsync)).GetCustomAttribute<HttpPostAttribute>()).Template);
        Assert.Equal("{visitId}/archive", Assert.IsType<HttpPostAttribute>(
            GetAction(nameof(PassportVisitsController.ArchiveAsync)).GetCustomAttribute<HttpPostAttribute>()).Template);
        Assert.Equal("{visitId}/deletion-preview", Assert.IsType<HttpGetAttribute>(
            GetAction(nameof(PassportVisitsController.GetDeletionPreviewAsync)).GetCustomAttribute<HttpGetAttribute>()).Template);
        Assert.Equal("{visitId}", Assert.IsType<HttpDeleteAttribute>(
            GetAction(nameof(PassportVisitsController.DeleteAsync)).GetCustomAttribute<HttpDeleteAttribute>()).Template);
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
        IQueryHandler<GetVisitQuery, ApplicationResult<VisitResult>>? getHandler = null,
        ICommandHandler<UpdateVisitMetadataCommand, ApplicationResult<VisitResult>>? updateHandler = null,
        ICommandHandler<CompleteVisitCommand, ApplicationResult<VisitResult>>? completeHandler = null,
        ICommandHandler<ReopenVisitCommand, ApplicationResult<VisitResult>>? reopenHandler = null,
        ICommandHandler<ArchiveVisitCommand, ApplicationResult<VisitResult>>? archiveHandler = null,
        IQueryHandler<GetVisitDeletionPreviewQuery, ApplicationResult<VisitDeletionPreview>>? deletionPreviewHandler = null,
        ICommandHandler<DeleteVisitCommand, ApplicationResult<VisitDeletionReceipt>>? deleteHandler = null)
    {
        return new PassportVisitsController(
            createHandler ?? new Mock<ICommandHandler<CreateVisitCommand, ApplicationResult<CreateVisitResult>>>(MockBehavior.Strict).Object,
            listHandler ?? new Mock<IQueryHandler<ListUserVisitsQuery, ApplicationResult<VisitPageResult>>>(MockBehavior.Strict).Object,
            getHandler ?? new Mock<IQueryHandler<GetVisitQuery, ApplicationResult<VisitResult>>>(MockBehavior.Strict).Object,
            updateHandler ?? new Mock<ICommandHandler<UpdateVisitMetadataCommand, ApplicationResult<VisitResult>>>(MockBehavior.Strict).Object,
            completeHandler ?? new Mock<ICommandHandler<CompleteVisitCommand, ApplicationResult<VisitResult>>>(MockBehavior.Strict).Object,
            reopenHandler ?? new Mock<ICommandHandler<ReopenVisitCommand, ApplicationResult<VisitResult>>>(MockBehavior.Strict).Object,
            archiveHandler ?? new Mock<ICommandHandler<ArchiveVisitCommand, ApplicationResult<VisitResult>>>(MockBehavior.Strict).Object,
            deletionPreviewHandler ?? new Mock<IQueryHandler<GetVisitDeletionPreviewQuery, ApplicationResult<VisitDeletionPreview>>>(MockBehavior.Strict).Object,
            deleteHandler ?? new Mock<ICommandHandler<DeleteVisitCommand, ApplicationResult<VisitDeletionReceipt>>>(MockBehavior.Strict).Object);
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
