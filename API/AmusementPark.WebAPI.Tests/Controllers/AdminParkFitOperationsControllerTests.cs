using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkFit;
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

public sealed class AdminParkFitOperationsControllerTests
{
    [Fact]
    public async Task GetReportsAsync_ShouldReturnTheMappedPage()
    {
        DateTime submittedAtUtc = new DateTime(2026, 9, 14, 8, 0, 0, DateTimeKind.Utc);
        PagedResult<ParkFitSourceReportResult> page = new PagedResult<ParkFitSourceReportResult>(
            new[]
            {
                new ParkFitSourceReportResult(
                    "7d283106-142f-49cf-adfb-2f2728466fa9",
                    "park-1",
                    "Parc test",
                    ParkFitEvidenceKind.OpeningCalendar,
                    "https://example.com/calendar",
                    null,
                    ParkFitSourceReportReason.Outdated,
                    "Horaires anciens",
                    ParkFitSourceReportStatus.Pending,
                    submittedAtUtc,
                    null,
                    null,
                    0),
            },
            1,
            20,
            1);
        Mock<IQueryHandler<
            GetParkFitSourceReportsQuery,
            ApplicationResult<PagedResult<ParkFitSourceReportResult>>>> queryHandler =
            new Mock<IQueryHandler<
                GetParkFitSourceReportsQuery,
                ApplicationResult<PagedResult<ParkFitSourceReportResult>>>>(MockBehavior.Strict);
        queryHandler.Setup(value => value.HandleAsync(
                It.Is<GetParkFitSourceReportsQuery>(query =>
                    query.Criteria.Paging.Page == 1
                    && query.Criteria.Paging.PageSize == 20
                    && query.Criteria.Status == ParkFitSourceReportStatus.Pending),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<PagedResult<ParkFitSourceReportResult>>.Success(page));
        AdminParkFitOperationsController controller = CreateController(queryHandler.Object);

        IActionResult response = await controller.GetReportsAsync(
            new ParkFitSourceReportSearchRequestDto { Status = "Pending" },
            CancellationToken.None);

        PagedResponseDto<ParkFitSourceReportDto> body =
            Assert.IsType<PagedResponseDto<ParkFitSourceReportDto>>(
                Assert.IsType<OkObjectResult>(response).Value);
        ParkFitSourceReportDto report = Assert.Single(body.Data);
        Assert.Equal("Parc test", report.ParkName);
        Assert.Equal("OpeningCalendar", report.EvidenceKind);
        queryHandler.VerifyAll();
    }

    [Fact]
    public async Task ReviewReportAsync_ShouldUseTheAuthenticatedAdministrator()
    {
        Mock<ICommandHandler<ReviewParkFitSourceReportCommand, ApplicationResult>> handler =
            new Mock<ICommandHandler<ReviewParkFitSourceReportCommand, ApplicationResult>>(
                MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<ReviewParkFitSourceReportCommand>(command =>
                    command.ReportId == "7d283106-142f-49cf-adfb-2f2728466fa9"
                    && command.ReviewerUserId == "admin-1"
                    && command.Decision == ParkFitSourceReportStatus.Resolved
                    && command.ExpectedRevision == 2),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult.Success());
        AdminParkFitOperationsController controller = CreateController(reportHandler: handler.Object);
        SetAuthenticatedUser(controller, "admin-1");

        IActionResult response = await controller.ReviewReportAsync(
            "7d283106-142f-49cf-adfb-2f2728466fa9",
            new ReviewParkFitSourceReportRequestDto
            {
                Decision = "Resolved",
                DecisionNote = "Source corrigée",
                ExpectedRevision = 2,
            },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(response);
        handler.VerifyAll();
    }

    [Fact]
    public async Task ChangeOperationalStatusAsync_ShouldUseTheAuthenticatedAdministrator()
    {
        Mock<ICommandHandler<ChangeParkFitOperationalStatusCommand, ApplicationResult>> handler =
            new Mock<ICommandHandler<ChangeParkFitOperationalStatusCommand, ApplicationResult>>(
                MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<ChangeParkFitOperationalStatusCommand>(command =>
                    command.ParkId == "park-1"
                    && command.ActorUserId == "admin-1"
                    && command.TargetState == ParkFitRecommendationState.Suspended
                    && command.Reason == "Calendrier à vérifier"
                    && command.ExpectedRevision == 0),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult.Success());
        AdminParkFitOperationsController controller = CreateController(statusHandler: handler.Object);
        SetAuthenticatedUser(controller, "admin-1");

        IActionResult response = await controller.ChangeOperationalStatusAsync(
            "park-1",
            new ChangeParkFitOperationalStatusRequestDto
            {
                TargetState = "Suspended",
                Reason = "Calendrier à vérifier",
                ExpectedRevision = 0,
            },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(response);
        handler.VerifyAll();
    }

    [Fact]
    public async Task ChangeOperationalStatusAsync_ShouldAcceptPortfolioDeactivation()
    {
        Mock<ICommandHandler<ChangeParkFitOperationalStatusCommand, ApplicationResult>> handler =
            new Mock<ICommandHandler<ChangeParkFitOperationalStatusCommand, ApplicationResult>>(
                MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<ChangeParkFitOperationalStatusCommand>(command =>
                    command.TargetState == ParkFitRecommendationState.NotActivated
                    && command.Reason == "Retrait du portefeuille"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult.Success());
        AdminParkFitOperationsController controller = CreateController(statusHandler: handler.Object);
        SetAuthenticatedUser(controller, "admin-1");

        IActionResult response = await controller.ChangeOperationalStatusAsync(
            "park-1",
            new ChangeParkFitOperationalStatusRequestDto
            {
                TargetState = "NotActivated",
                Reason = "Retrait du portefeuille",
                ExpectedRevision = 2,
            },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(response);
        handler.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldBeAdminOnlyNoStoreRateLimitedAndAudited()
    {
        AuthorizeAttribute authorization = Assert.Single(
            typeof(AdminParkFitOperationsController).GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => !string.IsNullOrWhiteSpace(attribute.Roles));
        RequireActivatedUnblockedUserAttribute activation = Assert.Single(
            typeof(AdminParkFitOperationsController)
                .GetCustomAttributes<RequireActivatedUnblockedUserAttribute>());
        ResponseCacheAttribute cache = Assert.Single(
            typeof(AdminParkFitOperationsController).GetCustomAttributes<ResponseCacheAttribute>());
        MethodInfo reviewAction = typeof(AdminParkFitOperationsController)
            .GetMethod(nameof(AdminParkFitOperationsController.ReviewReportAsync))!;
        MethodInfo statusAction = typeof(AdminParkFitOperationsController)
            .GetMethod(nameof(AdminParkFitOperationsController.ChangeOperationalStatusAsync))!;

        Assert.Equal(AuthorizationRoleGroups.Admin, authorization.Roles);
        Assert.Equal(AuthorizationPolicyNames.ActivatedUnblockedUser, activation.Policy);
        Assert.True(cache.NoStore);
        Assert.Equal(
            RateLimitPolicyNames.ParkFitAdministration,
            Assert.Single(reviewAction.GetCustomAttributes<EnableRateLimitingAttribute>()).PolicyName);
        Assert.Equal(
            "park-fit.report.review",
            Assert.Single(reviewAction.GetCustomAttributes<AdminAuditAttribute>()).Action);
        Assert.Equal(
            RateLimitPolicyNames.ParkFitAdministration,
            Assert.Single(statusAction.GetCustomAttributes<EnableRateLimitingAttribute>()).PolicyName);
        Assert.Equal(
            "park-fit.operational-status.change",
            Assert.Single(statusAction.GetCustomAttributes<AdminAuditAttribute>()).Action);
    }

    private static AdminParkFitOperationsController CreateController(
        IQueryHandler<
            GetParkFitSourceReportsQuery,
            ApplicationResult<PagedResult<ParkFitSourceReportResult>>>? queryHandler = null,
        ICommandHandler<ReviewParkFitSourceReportCommand, ApplicationResult>? reportHandler = null,
        ICommandHandler<ChangeParkFitOperationalStatusCommand, ApplicationResult>? statusHandler = null)
    {
        return new AdminParkFitOperationsController(
            queryHandler ?? Mock.Of<IQueryHandler<
                GetParkFitSourceReportsQuery,
                ApplicationResult<PagedResult<ParkFitSourceReportResult>>>>(),
            reportHandler ?? Mock.Of<ICommandHandler<ReviewParkFitSourceReportCommand, ApplicationResult>>(),
            statusHandler ?? Mock.Of<ICommandHandler<ChangeParkFitOperationalStatusCommand, ApplicationResult>>());
    }

    private static void SetAuthenticatedUser(
        AdminParkFitOperationsController controller,
        string userId)
    {
        ClaimsIdentity identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
            "test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity),
            },
        };
    }
}
