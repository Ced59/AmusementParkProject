using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport;
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

public sealed class PassportRideOccurrencesControllerTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 3, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AddAsync_ShouldUseOwnerHeaderAndCreateOneOccurrence()
    {
        Mock<ICommandHandler<AddRideOccurrencesBatchCommand, ApplicationResult<CreateRideOccurrencesResult>>> add =
            new Mock<ICommandHandler<AddRideOccurrencesBatchCommand, ApplicationResult<CreateRideOccurrencesResult>>>(MockBehavior.Strict);
        add.Setup(handler => handler.HandleAsync(
                It.Is<AddRideOccurrencesBatchCommand>(command =>
                    command.UserId == "owner-1"
                    && command.VisitId == "visit-1"
                    && command.ClientOperationId == "request-1"
                    && command.Source == RideLogSource.Manual
                    && command.Items.Single() != null
                    && command.Items.Single()!.Count == 1),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<CreateRideOccurrencesResult>.Success(
                new CreateRideOccurrencesResult(
                    new[] { CreateResult("occurrence-1") },
                    false,
                    true)));
        PassportRideOccurrencesController controller = CreateController(add.Object);
        controller.ControllerContext = CreateControllerContext();
        controller.Request.PathBase = "/api";

        IActionResult result = await controller.AddAsync(
            "visit-1",
            new CreatePassportRideOccurrenceRequestDto
            {
                ParkItemId = "item-1",
            },
            "request-1",
            CancellationToken.None);

        CreatedResult created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(
            "/api/me/passport/visits/visit-1/occurrences/occurrence-1",
            created.Location);
        PassportRideOccurrenceDto body = Assert.IsType<PassportRideOccurrenceDto>(
            created.Value);
        Assert.Equal("occurrence-1", body.Id);
        Assert.Null(typeof(PassportRideOccurrenceDto).GetProperty("UserId"));
        Assert.Equal("true", controller.Response.Headers["Ride-Order-Normalized"]);
        add.VerifyAll();
    }

    [Fact]
    public async Task AddBatchAsync_WhenNormalized_ShouldExposeOrderHeader()
    {
        Mock<ICommandHandler<AddRideOccurrencesBatchCommand, ApplicationResult<CreateRideOccurrencesResult>>> add =
            new Mock<ICommandHandler<AddRideOccurrencesBatchCommand, ApplicationResult<CreateRideOccurrencesResult>>>(MockBehavior.Strict);
        add.Setup(handler => handler.HandleAsync(
                It.IsAny<AddRideOccurrencesBatchCommand>(),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<CreateRideOccurrencesResult>.Success(
                new CreateRideOccurrencesResult(
                    new[]
                    {
                        CreateResult("occurrence-1"),
                        CreateResult("occurrence-2"),
                    },
                    false,
                    true)));
        PassportRideOccurrencesController controller = CreateController(add.Object);
        controller.ControllerContext = CreateControllerContext();

        IActionResult result = await controller.AddBatchAsync(
            "visit-1",
            new CreatePassportRideOccurrencesBatchRequestDto
            {
                Items = new List<CreatePassportRideOccurrenceBatchItemDto>
                {
                    new CreatePassportRideOccurrenceBatchItemDto
                    {
                        ParkItemId = "item-1",
                        Count = 2,
                    },
                },
            },
            "request-1",
            CancellationToken.None);

        ObjectResult created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal("true", controller.Response.Headers["Ride-Order-Normalized"]);
        add.VerifyAll();
    }

    [Fact]
    public async Task ImportBatchAsync_ShouldMarkTheApplicationCommandAsAnImport()
    {
        Mock<ICommandHandler<AddRideOccurrencesBatchCommand, ApplicationResult<CreateRideOccurrencesResult>>> add =
            new Mock<ICommandHandler<AddRideOccurrencesBatchCommand, ApplicationResult<CreateRideOccurrencesResult>>>(MockBehavior.Strict);
        add.Setup(handler => handler.HandleAsync(
                It.Is<AddRideOccurrencesBatchCommand>(command =>
                    command.UserId == "owner-1"
                    && command.VisitId == "visit-1"
                    && command.ClientOperationId == "import-1"
                    && command.Source == RideLogSource.Import),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<CreateRideOccurrencesResult>.Success(
                new CreateRideOccurrencesResult(
                    new[] { CreateResult("occurrence-1") },
                    false,
                    false)));
        PassportRideOccurrencesController controller = CreateController(add.Object);
        controller.ControllerContext = CreateControllerContext();

        IActionResult result = await controller.ImportBatchAsync(
            "visit-1",
            new CreatePassportRideOccurrencesBatchRequestDto
            {
                Items = new List<CreatePassportRideOccurrenceBatchItemDto>
                {
                    new CreatePassportRideOccurrenceBatchItemDto { ParkItemId = "item-1" },
                },
            },
            "import-1",
            CancellationToken.None);

        ObjectResult created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        add.VerifyAll();
    }

    [Fact]
    public async Task AddBatchAsync_WithNullItem_ShouldReturnBoundedValidationFailure()
    {
        Mock<ICommandHandler<AddRideOccurrencesBatchCommand, ApplicationResult<CreateRideOccurrencesResult>>> add =
            new Mock<ICommandHandler<AddRideOccurrencesBatchCommand, ApplicationResult<CreateRideOccurrencesResult>>>(MockBehavior.Strict);
        add.Setup(handler => handler.HandleAsync(
                It.Is<AddRideOccurrencesBatchCommand>(command =>
                    command.Items.Count == 1
                    && command.Items.Single() == null),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<CreateRideOccurrencesResult>.Failure(
                PassportApplicationErrors.InvalidRideOccurrenceBatch()));
        PassportRideOccurrencesController controller = CreateController(add.Object);
        controller.ControllerContext = CreateControllerContext();
        CreatePassportRideOccurrencesBatchRequestDto request =
            JsonSerializer.Deserialize<CreatePassportRideOccurrencesBatchRequestDto>(
                "{\"items\":[null]}",
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException();

        IActionResult result = await controller.AddBatchAsync(
            "visit-1",
            request,
            "request-1",
            CancellationToken.None);

        ObjectResult problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal(
            "ride-occurrence.batch-invalid",
            Assert.IsType<ProblemDetails>(problem.Value).Extensions["errorCode"]);
        add.VerifyAll();
    }

    [Fact]
    public async Task ListAsync_WithInvalidCursor_ShouldFailBeforeApplication()
    {
        Mock<IQueryHandler<ListRideOccurrencesQuery, ApplicationResult<RideOccurrencePageResult>>> list =
            new Mock<IQueryHandler<ListRideOccurrencesQuery, ApplicationResult<RideOccurrencePageResult>>>(MockBehavior.Strict);
        PassportRideOccurrencesController controller = CreateController(listHandler: list.Object);
        controller.ControllerContext = CreateControllerContext();

        IActionResult result = await controller.ListAsync(
            "visit-1",
            new PassportRideOccurrenceListRequestDto { Cursor = "invalid!" },
            CancellationToken.None);

        ObjectResult problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal(
            "ride-occurrence.cursor.invalid",
            Assert.IsType<ProblemDetails>(problem.Value).Extensions["errorCode"]);
        list.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_ShouldReadTheOwnedOccurrenceAtTheCreatedLocation()
    {
        Mock<IQueryHandler<GetRideOccurrenceQuery, ApplicationResult<RideOccurrenceResult>>> get =
            new Mock<IQueryHandler<GetRideOccurrenceQuery, ApplicationResult<RideOccurrenceResult>>>(MockBehavior.Strict);
        get.Setup(handler => handler.HandleAsync(
                It.Is<GetRideOccurrenceQuery>(query =>
                    query.UserId == "owner-1"
                    && query.VisitId == "visit-1"
                    && query.OccurrenceId == "occurrence-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<RideOccurrenceResult>.Success(
                CreateResult("occurrence-1")));
        PassportRideOccurrencesController controller = CreateController(
            getHandler: get.Object);
        controller.ControllerContext = CreateControllerContext();

        IActionResult result = await controller.GetAsync(
            "visit-1",
            "occurrence-1",
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        PassportRideOccurrenceDto body = Assert.IsType<PassportRideOccurrenceDto>(ok.Value);
        Assert.Equal("occurrence-1", body.Id);
        Assert.Equal("Current ride name", body.Target?.Name);
        Assert.Equal("Attraction", body.Target?.Category);
        Assert.Equal("Operating", body.Target?.LifecycleStatus);
        Assert.False(body.Target!.IsHistoricalSnapshot);
        get.VerifyAll();
    }

    [Fact]
    public async Task ReorderAsync_WhenReplayedAndNormalized_ShouldExposeDiagnostics()
    {
        Mock<ICommandHandler<ReorderRideOccurrenceCommand, ApplicationResult<ReorderRideOccurrenceResult>>> reorder =
            new Mock<ICommandHandler<ReorderRideOccurrenceCommand, ApplicationResult<ReorderRideOccurrenceResult>>>(MockBehavior.Strict);
        reorder.Setup(handler => handler.HandleAsync(
                It.IsAny<ReorderRideOccurrenceCommand>(),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ReorderRideOccurrenceResult>.Success(
                new ReorderRideOccurrenceResult(
                    CreateResult("occurrence-1"),
                    true,
                    true)));
        PassportRideOccurrencesController controller = CreateController(
            reorderHandler: reorder.Object);
        controller.ControllerContext = CreateControllerContext();

        IActionResult result = await controller.ReorderAsync(
            "visit-1",
            new ReorderPassportRideOccurrenceRequestDto
            {
                OccurrenceId = "occurrence-1",
                ExpectedVersion = 1,
                Placement = PassportRideOccurrencePlacementDto.Last,
            },
            "request-1",
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("true", controller.Response.Headers["Idempotency-Replayed"]);
        Assert.Equal("true", controller.Response.Headers["Ride-Order-Normalized"]);
        reorder.VerifyAll();
    }

    [Fact]
    public void UpdateRequest_ShouldRejectMissingVersionAndStatusDuringModelValidation()
    {
        UpdatePassportRideOccurrenceRequestDto request =
            new UpdatePassportRideOccurrenceRequestDto();
        List<ValidationResult> results = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Contains("ExpectedVersion"));
        Assert.Contains(results, result => result.MemberNames.Contains("Status"));
    }

    [Fact]
    public void Controller_ShouldExposeOnlyPrivateNoStoreRoutes()
    {
        RouteAttribute route = Assert.IsType<RouteAttribute>(
            typeof(PassportRideOccurrencesController).GetCustomAttribute<RouteAttribute>());
        Assert.Equal("me/passport/visits/{visitId}", route.Template);
        AuthorizeAttribute authorize = Assert.Single(
            typeof(PassportRideOccurrencesController).GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.UserModeratorAdmin, authorize.Roles);
        Assert.NotNull(typeof(PassportRideOccurrencesController)
            .GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
            typeof(PassportRideOccurrencesController)
                .GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);

        Assert.Equal(
            "occurrences:batch",
            GetAction(nameof(PassportRideOccurrencesController.AddBatchAsync))
                .GetCustomAttribute<HttpPostAttribute>()?.Template);
        Assert.Equal(
            "occurrences/{occurrenceId}",
            GetAction(nameof(PassportRideOccurrencesController.GetAsync))
                .GetCustomAttribute<HttpGetAttribute>()?.Template);
        Assert.Equal(
            "occurrences:reorder",
            GetAction(nameof(PassportRideOccurrencesController.ReorderAsync))
                .GetCustomAttribute<HttpPostAttribute>()?.Template);
        ParameterInfo header = GetAction(nameof(PassportRideOccurrencesController.ReorderAsync))
            .GetParameters()
            .Single(static parameter => parameter.Name == "idempotencyKey");
        Assert.Equal(
            "Idempotency-Key",
            header.GetCustomAttribute<FromHeaderAttribute>()?.Name);
        Assert.NotNull(header.GetCustomAttribute<RequiredAttribute>());
    }

    private static MethodInfo GetAction(string name)
    {
        return typeof(PassportRideOccurrencesController).GetMethod(name)
            ?? throw new InvalidOperationException();
    }

    private static PassportRideOccurrencesController CreateController(
        ICommandHandler<AddRideOccurrencesBatchCommand, ApplicationResult<CreateRideOccurrencesResult>>? addHandler = null,
        ICommandHandler<ReorderRideOccurrenceCommand, ApplicationResult<ReorderRideOccurrenceResult>>? reorderHandler = null,
        IQueryHandler<GetRideOccurrenceQuery, ApplicationResult<RideOccurrenceResult>>? getHandler = null,
        IQueryHandler<ListRideOccurrencesQuery, ApplicationResult<RideOccurrencePageResult>>? listHandler = null)
    {
        return new PassportRideOccurrencesController(
            addHandler ?? new Mock<ICommandHandler<AddRideOccurrencesBatchCommand, ApplicationResult<CreateRideOccurrencesResult>>>(MockBehavior.Strict).Object,
            new Mock<ICommandHandler<UpdateRideOccurrenceCommand, ApplicationResult<RideOccurrenceResult>>>(MockBehavior.Strict).Object,
            new Mock<ICommandHandler<DeleteRideOccurrenceCommand, ApplicationResult<RideOccurrenceResult>>>(MockBehavior.Strict).Object,
            reorderHandler ?? new Mock<ICommandHandler<ReorderRideOccurrenceCommand, ApplicationResult<ReorderRideOccurrenceResult>>>(MockBehavior.Strict).Object,
            getHandler ?? new Mock<IQueryHandler<GetRideOccurrenceQuery, ApplicationResult<RideOccurrenceResult>>>(MockBehavior.Strict).Object,
            listHandler ?? new Mock<IQueryHandler<ListRideOccurrencesQuery, ApplicationResult<RideOccurrencePageResult>>>(MockBehavior.Strict).Object);
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

    private static RideOccurrenceResult CreateResult(string id)
    {
        return new RideOccurrenceResult(
            id,
            "visit-1",
            "park-1",
            "item-1",
            1024,
            new RideOccurrenceMomentResult(null, false),
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
            null,
            true,
            1,
            NowUtc,
            NowUtc,
            new RideOccurrenceTargetResult(
                "Current ride name",
                "Attraction",
                "Operating",
                false));
    }
}
