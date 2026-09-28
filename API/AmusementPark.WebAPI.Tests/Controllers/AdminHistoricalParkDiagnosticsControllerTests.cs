using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.History;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class AdminHistoricalParkDiagnosticsControllerTests
{
    [Fact]
    public void Controller_ShouldBeAdminOnlyActivatedAndNonCacheable()
    {
        Type type = typeof(AdminHistoricalParkDiagnosticsController);

        Assert.Equal(
            "admin/history/parks/{parkId}/diagnostics",
            type.GetCustomAttribute<RouteAttribute>()?.Template);
        AuthorizeAttribute authorize = Assert.Single(
            type.GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.Admin, authorize.Roles);
        Assert.NotNull(type.GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        Assert.True(type.GetCustomAttribute<ResponseCacheAttribute>()?.NoStore);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnTheMappedDiagnosticWithoutPrivateVisitData()
    {
        Guid factId = Guid.NewGuid();
        HistoricalSubject subject = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject);
        HistoricalParkDiagnostics diagnostics = new HistoricalParkDiagnostics(
            2,
            1,
            new[]
            {
                new HistoricalParkDiagnosticIssue(
                    HistoricalDiagnosticCode.MissingSource,
                    HistoricalDiagnosticSeverity.Warning,
                    subject,
                    factId,
                    null),
            },
            new[] { new HistoricalDecadeCoverage(1990, 2, 1, 1, 1) },
            new[]
            {
                new HistoricalWorkflowStageCount(
                    HistoricalEditorialWorkflowState.EditorialReview,
                    3),
            });
        AdminHistoricalParkDiagnosticsResult expected = new AdminHistoricalParkDiagnosticsResult(
            "park-1",
            "Parc témoin",
            diagnostics,
            new HistoricalVisitDiagnosticCounts(5, 2, 4),
            new HistoricalParkRolloutGate(2, 2, 1, new[] { 1998 }));
        Mock<IQueryHandler<
            GetAdminHistoricalParkDiagnosticsQuery,
            ApplicationResult<AdminHistoricalParkDiagnosticsResult>>> handler =
            new Mock<IQueryHandler<
                GetAdminHistoricalParkDiagnosticsQuery,
                ApplicationResult<AdminHistoricalParkDiagnosticsResult>>>(MockBehavior.Strict);
        handler.Setup(candidate => candidate.HandleAsync(
                It.Is<GetAdminHistoricalParkDiagnosticsQuery>(query => query.ParkId == "park-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<AdminHistoricalParkDiagnosticsResult>.Success(expected));
        AdminHistoricalParkDiagnosticsController controller = new AdminHistoricalParkDiagnosticsController(
            handler.Object);

        IActionResult action = await controller.GetAsync("park-1", CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(action);
        AdminHistoricalParkDiagnosticsDto response =
            Assert.IsType<AdminHistoricalParkDiagnosticsDto>(ok.Value);
        Assert.Equal("Parc témoin", response.ParkName);
        Assert.Equal(0, response.BlockingIssueCount);
        Assert.Equal("MissingSource", Assert.Single(response.Issues).Code);
        Assert.Equal(5, response.Visits.PotentiallyInconsistentVisitCount);
        Assert.True(response.RolloutGate.IsOpen);
        Assert.Equal(new[] { 1998 }, response.RolloutGate.IndexableKeyYears);
        Assert.DoesNotContain("user", response.GetType().GetProperties()
            .Select(static property => property.Name), StringComparer.OrdinalIgnoreCase);
        handler.VerifyAll();
    }
}
