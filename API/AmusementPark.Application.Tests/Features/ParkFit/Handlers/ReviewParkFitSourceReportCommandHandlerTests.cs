using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Handlers;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Core.Domain.ParkFit;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkFit.Handlers;

public sealed class ReviewParkFitSourceReportCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldResolveTheExpectedReportRevision()
    {
        DateTime submittedAtUtc = new DateTime(2026, 9, 14, 8, 0, 0, DateTimeKind.Utc);
        ParkFitSourceReport report = ParkFitSourceReport.Create(
            ParkFitSourceReportId.Parse("7d283106-142f-49cf-adfb-2f2728466fa9"),
            "park-1",
            "Parc témoin",
            ParkFitEvidenceKind.OpeningCalendar,
            "https://example.org/calendar",
            null,
            ParkFitSourceReportReason.Outdated,
            null,
            submittedAtUtc);
        Mock<IParkFitSourceReportRepository> repository =
            new Mock<IParkFitSourceReportRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetAsync(report.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
        repository.Setup(value => value.ReplaceAsync(
                It.Is<ParkFitSourceReport>(candidate =>
                    candidate.Status == ParkFitSourceReportStatus.Resolved
                    && candidate.ReviewedByUserId == "admin-1"
                    && candidate.DecisionNote == "Source corrigée"
                    && candidate.Revision == 1),
                0,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ParkFitSourceReportWriteOutcome.Success);
        ReviewParkFitSourceReportCommandHandler handler =
            new ReviewParkFitSourceReportCommandHandler(
                repository.Object,
                new SearchParksByFitQueryHandlerTestsFixedTimeProvider(
                    new DateTimeOffset(submittedAtUtc.AddHours(1))));

        ApplicationResult result = await handler.HandleAsync(
            new ReviewParkFitSourceReportCommand(
                report.Id.Value,
                ParkFitSourceReportStatus.Resolved,
                "admin-1",
                "Source corrigée",
                0));

        Assert.True(result.IsSuccess);
        repository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WithStaleRevision_ShouldReturnConflictWithoutWriting()
    {
        DateTime submittedAtUtc = new DateTime(2026, 9, 14, 8, 0, 0, DateTimeKind.Utc);
        ParkFitSourceReport report = ParkFitSourceReport.Create(
            ParkFitSourceReportId.Parse("7d283106-142f-49cf-adfb-2f2728466fa9"),
            "park-1",
            "Parc témoin",
            ParkFitEvidenceKind.OpeningCalendar,
            null,
            "Calendrier 2026",
            ParkFitSourceReportReason.Outdated,
            null,
            submittedAtUtc);
        Mock<IParkFitSourceReportRepository> repository =
            new Mock<IParkFitSourceReportRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetAsync(report.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
        ReviewParkFitSourceReportCommandHandler handler =
            new ReviewParkFitSourceReportCommandHandler(repository.Object);

        ApplicationResult result = await handler.HandleAsync(
            new ReviewParkFitSourceReportCommand(
                report.Id.Value,
                ParkFitSourceReportStatus.Dismissed,
                "admin-1",
                null,
                1));

        Assert.False(result.IsSuccess);
        Assert.Equal("park-fit.operations.conflict", Assert.Single(result.Errors).Code);
        repository.VerifyAll();
    }
}
