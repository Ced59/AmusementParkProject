using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.WebAPI.Contracts.History;
using AmusementPark.WebAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class ParkDataEditorHistoryDiagnosticsControllerTests
{
    [Fact]
    public async Task GetAsync_ShouldReturnHistoricalRolloutGateDiagnostics()
    {
        HistoricalParkDiagnostics diagnostics = new HistoricalParkDiagnostics(
            3,
            0,
            Array.Empty<HistoricalParkDiagnosticIssue>(),
            Array.Empty<HistoricalDecadeCoverage>(),
            Array.Empty<HistoricalWorkflowStageCount>());
        HistoricalParkRolloutGate rolloutGate = new HistoricalParkRolloutGate(
            3,
            2,
            1,
            new[] { 1967 });
        AdminHistoricalParkDiagnosticsResult response = new AdminHistoricalParkDiagnosticsResult(
            "park-1",
            "Park 1",
            diagnostics,
            new HistoricalVisitDiagnosticCounts(0, 0, 0),
            rolloutGate);
        Mock<IQueryHandler<
            GetAdminHistoricalParkDiagnosticsQuery,
            ApplicationResult<AdminHistoricalParkDiagnosticsResult>>> handler = new(
                MockBehavior.Strict);
        handler
            .Setup(candidate => candidate.HandleAsync(
                It.Is<GetAdminHistoricalParkDiagnosticsQuery>(query => query.ParkId == "park-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<AdminHistoricalParkDiagnosticsResult>.Success(response));
        ParkDataEditorHistoryDiagnosticsController controller = new(handler.Object);

        IActionResult result = await controller.GetAsync("park-1", CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        AdminHistoricalParkDiagnosticsDto dto =
            Assert.IsType<AdminHistoricalParkDiagnosticsDto>(ok.Value);
        Assert.Equal("park-1", dto.ParkId);
        Assert.Equal(3, dto.RolloutGate.PublishedFactCount);
        Assert.Equal(2, dto.RolloutGate.SourcedFactCount);
        Assert.Equal(1, dto.RolloutGate.MajorFactCount);
        Assert.False(dto.RolloutGate.IsOpen);
        handler.VerifyAll();
    }
}
