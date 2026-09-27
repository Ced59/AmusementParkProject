using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.HistoricalExistenceReports.Commands;
using AmusementPark.Application.Features.HistoricalExistenceReports.Handlers;
using AmusementPark.Application.Features.HistoricalExistenceReports.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.HistoricalExistenceReports.Handlers;

public sealed class ReviewHistoricalExistenceReportCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldAcceptForResearchWithoutPublishingACanonicalFact()
    {
        HistoricalExistenceReport report = CreateReport();
        Mock<IHistoricalExistenceReportRepository> repository = new(MockBehavior.Strict);
        repository.Setup(value => value.GetAsync(report.Id, CancellationToken.None))
            .ReturnsAsync(report);
        repository.Setup(value => value.ReplaceAsync(
                It.Is<HistoricalExistenceReport>(candidate =>
                    candidate.Status == HistoricalExistenceReportStatus.AcceptedForResearch
                    && candidate.Revision == 1),
                0,
                CancellationToken.None))
            .ReturnsAsync(HistoricalExistenceReportWriteOutcome.Success);
        ReviewHistoricalExistenceReportCommandHandler handler = new(repository.Object);

        ApplicationResult result = await handler.HandleAsync(
            new ReviewHistoricalExistenceReportCommand(
                report.Id.Value,
                HistoricalExistenceReportStatus.AcceptedForResearch,
                "admin-1",
                "À recouper",
                0));

        Assert.True(result.IsSuccess);
        repository.VerifyAll();
    }

    private static HistoricalExistenceReport CreateReport()
    {
        return HistoricalExistenceReport.Create(
            HistoricalExistenceReportId.New(),
            "owner-1",
            VisitId.Parse("visit-1"),
            "park-1",
            "Parc témoin",
            VisitDate.ForYear(1998),
            "Ancien Cyclone",
            null,
            null,
            null,
            new DateTime(2026, 9, 27, 8, 0, 0, DateTimeKind.Utc));
    }
}
