using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.WebAPI.Contracts.ParkFit;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class PublicParkFitControllerTests
{
    [Fact]
    public async Task SearchAsync_ShouldForwardAnonymousCriteriaAndReturnTheBoundedResult()
    {
        DateOnly evaluationDate = new DateOnly(2026, 10, 10);
        ParkFitSearchResult applicationResult = new ParkFitSearchResult
        {
            MethodVersion = "park-fit-2026-01",
            EvaluationDate = evaluationDate,
            TotalCandidateCount = 2,
            InspectedCandidateCount = 2,
        };
        Mock<IQueryHandler<
            SearchParksByFitQuery,
            ApplicationResult<ParkFitSearchResult>>> handler =
            new Mock<IQueryHandler<
                SearchParksByFitQuery,
                ApplicationResult<ParkFitSearchResult>>>(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<SearchParksByFitQuery>(query =>
                    query.EvaluationDate == evaluationDate
                    && query.Members.Single().MemberKey == "member-1"
                    && query.MaximumResults == 5),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ParkFitSearchResult>.Success(applicationResult));
        Mock<ICommandHandler<SubmitParkFitSourceReportCommand, ApplicationResult>> reportHandler =
            new Mock<ICommandHandler<SubmitParkFitSourceReportCommand, ApplicationResult>>(
                MockBehavior.Strict);
        PublicParkFitController controller = new PublicParkFitController(
            handler.Object,
            reportHandler.Object,
            BuildPilotHandler().Object);
        ParkFitSearchRequestDto request = new ParkFitSearchRequestDto
        {
            EvaluationDate = evaluationDate,
            Members = new[]
            {
                new ParkFitSearchMemberCriteriaDto
                {
                    HeightCentimeters = 120,
                },
            },
            MaximumResults = 5,
        };

        IActionResult response = await controller.SearchAsync(request, CancellationToken.None);

        ParkFitSearchResponseDto body = Assert.IsType<ParkFitSearchResponseDto>(
            Assert.IsType<OkObjectResult>(response).Value);
        Assert.Equal(2, body.TotalCandidateCount);
        Assert.Equal("park-fit-2026-01", body.MethodVersion);
        handler.VerifyAll();
    }

    [Fact]
    public async Task ReportAsync_WithValidRequest_ShouldSubmitAnAnonymousReport()
    {
        Mock<IQueryHandler<
            SearchParksByFitQuery,
            ApplicationResult<ParkFitSearchResult>>> searchHandler =
            new Mock<IQueryHandler<
                SearchParksByFitQuery,
                ApplicationResult<ParkFitSearchResult>>>(MockBehavior.Strict);
        Mock<ICommandHandler<SubmitParkFitSourceReportCommand, ApplicationResult>> reportHandler =
            new Mock<ICommandHandler<SubmitParkFitSourceReportCommand, ApplicationResult>>(
                MockBehavior.Strict);
        reportHandler.Setup(value => value.HandleAsync(
                It.Is<SubmitParkFitSourceReportCommand>(command =>
                    command.ParkId == "park-1"
                    && command.EvidenceKind == ParkFitEvidenceKind.OpeningCalendar
                    && command.Reason == ParkFitSourceReportReason.Outdated
                    && command.SourceUrl == "https://example.com/calendar"
                    && command.Details == "Horaires anciens"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult.Success());
        PublicParkFitController controller = new PublicParkFitController(
            searchHandler.Object,
            reportHandler.Object,
            BuildPilotHandler().Object);
        SubmitParkFitSourceReportRequestDto request = new SubmitParkFitSourceReportRequestDto
        {
            ParkId = "park-1",
            EvidenceKind = "OpeningCalendar",
            SourceUrl = "https://example.com/calendar",
            Reason = "Outdated",
            Details = "Horaires anciens",
        };

        IActionResult response = await controller.ReportAsync(request, CancellationToken.None);

        Assert.IsType<AcceptedResult>(response);
        reportHandler.VerifyAll();
    }

    [Fact]
    public async Task ReportAsync_WithUnknownTechnicalEnumValue_ShouldReturnBadRequest()
    {
        Mock<IQueryHandler<
            SearchParksByFitQuery,
            ApplicationResult<ParkFitSearchResult>>> searchHandler =
            new Mock<IQueryHandler<
                SearchParksByFitQuery,
                ApplicationResult<ParkFitSearchResult>>>(MockBehavior.Strict);
        Mock<ICommandHandler<SubmitParkFitSourceReportCommand, ApplicationResult>> reportHandler =
            new Mock<ICommandHandler<SubmitParkFitSourceReportCommand, ApplicationResult>>(
                MockBehavior.Strict);
        PublicParkFitController controller = new PublicParkFitController(
            searchHandler.Object,
            reportHandler.Object,
            BuildPilotHandler().Object);

        IActionResult response = await controller.ReportAsync(
            new SubmitParkFitSourceReportRequestDto
            {
                ParkId = "park-1",
                EvidenceKind = "999",
                Reason = "Outdated",
            },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(response);
        reportHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public void Controller_ShouldBePublicNoStoreAndRateLimited()
    {
        RouteAttribute route = Assert.IsType<RouteAttribute>(
            typeof(PublicParkFitController).GetCustomAttribute<RouteAttribute>());
        Assert.Equal("public/park-fit", route.Template);
        Assert.NotNull(typeof(PublicParkFitController)
            .GetCustomAttribute<AllowAnonymousAttribute>());
        ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
            typeof(PublicParkFitController).GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);

        MethodInfo action = typeof(PublicParkFitController).GetMethod(
            nameof(PublicParkFitController.SearchAsync))!;
        HttpPostAttribute post = Assert.IsType<HttpPostAttribute>(
            action.GetCustomAttribute<HttpPostAttribute>());
        Assert.Equal("search", post.Template);
        EnableRateLimitingAttribute rateLimit = Assert.IsType<EnableRateLimitingAttribute>(
            action.GetCustomAttribute<EnableRateLimitingAttribute>());
        Assert.Equal(RateLimitPolicyNames.ParkFitSearch, rateLimit.PolicyName);

        MethodInfo reportAction = typeof(PublicParkFitController).GetMethod(
            nameof(PublicParkFitController.ReportAsync))!;
        HttpPostAttribute reportPost = Assert.IsType<HttpPostAttribute>(
            reportAction.GetCustomAttribute<HttpPostAttribute>());
        Assert.Equal("reports", reportPost.Template);
        EnableRateLimitingAttribute reportRateLimit = Assert.IsType<EnableRateLimitingAttribute>(
            reportAction.GetCustomAttribute<EnableRateLimitingAttribute>());
        Assert.Equal(RateLimitPolicyNames.ParkFitReports, reportRateLimit.PolicyName);

        MethodInfo pilotAction = typeof(PublicParkFitController).GetMethod(
            nameof(PublicParkFitController.CapturePilotObservationAsync))!;
        HttpPostAttribute pilotPost = Assert.IsType<HttpPostAttribute>(
            pilotAction.GetCustomAttribute<HttpPostAttribute>());
        Assert.Equal("pilot-events", pilotPost.Template);
        EnableRateLimitingAttribute pilotRateLimit = Assert.IsType<EnableRateLimitingAttribute>(
            pilotAction.GetCustomAttribute<EnableRateLimitingAttribute>());
        Assert.Equal(RateLimitPolicyNames.ParkFitPilotEvents, pilotRateLimit.PolicyName);
    }

    [Fact]
    public async Task CapturePilotObservationAsync_ShouldAcceptOnlyAggregateDimensions()
    {
        Mock<IQueryHandler<
            SearchParksByFitQuery,
            ApplicationResult<ParkFitSearchResult>>> searchHandler =
            new(MockBehavior.Strict);
        Mock<ICommandHandler<SubmitParkFitSourceReportCommand, ApplicationResult>> reportHandler =
            new(MockBehavior.Strict);
        Mock<ICommandHandler<CaptureParkFitPilotObservationCommand, ApplicationResult>> pilotHandler =
            BuildPilotHandler();
        pilotHandler.Setup(handler => handler.HandleAsync(
                It.Is<CaptureParkFitPilotObservationCommand>(command =>
                    command.EventKind == ParkFitPilotEventKind.SearchCompleted
                    && command.ResultBand == ParkFitPilotResultBand.TwoToFour
                    && command.QualityIssues.Single()
                        == AmusementPark.Core.Domain.Parks.ParkFitDataQualityIssue.StaleEvidence),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult.Success());
        PublicParkFitController controller = new(
            searchHandler.Object,
            reportHandler.Object,
            pilotHandler.Object);

        IActionResult response = await controller.CapturePilotObservationAsync(
            new CaptureParkFitPilotObservationRequestDto
            {
                EventKind = "SearchCompleted",
                ResultBand = "TwoToFour",
                UnknownLevel = "Limited",
                DurationBand = "UnderOneAndHalfSeconds",
                MethodVersion = "park-fit-2026-01",
                QualityIssues = ["StaleEvidence"],
            },
            CancellationToken.None);

        Assert.IsType<AcceptedResult>(response);
        pilotHandler.VerifyAll();
    }

    private static Mock<ICommandHandler<CaptureParkFitPilotObservationCommand, ApplicationResult>>
        BuildPilotHandler()
    {
        return new Mock<ICommandHandler<CaptureParkFitPilotObservationCommand, ApplicationResult>>(
            MockBehavior.Strict);
    }
}
