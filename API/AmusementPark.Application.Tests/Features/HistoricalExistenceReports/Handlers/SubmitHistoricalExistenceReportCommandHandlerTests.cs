using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.HistoricalExistenceReports.Commands;
using AmusementPark.Application.Features.HistoricalExistenceReports.Handlers;
using AmusementPark.Application.Features.HistoricalExistenceReports.Ports;
using AmusementPark.Application.Features.HistoricalExistenceReports.Results;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.HistoricalExistenceReports.Handlers;

public sealed class SubmitHistoricalExistenceReportCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldSnapshotOnlyOwnedVisitContext()
    {
        Visit visit = CreateVisit();
        Park park = new Park { Id = visit.ParkId, Name = "Parc témoin" };
        Mock<IUserVisitRepository> visits = new(MockBehavior.Strict);
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        Mock<IHistoricalExistenceReportRepository> reports = new(MockBehavior.Strict);
        visits.Setup(repository => repository.GetOwnedAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(visit);
        parks.Setup(repository => repository.GetByIdAsync(
                visit.ParkId,
                true,
                CancellationToken.None))
            .ReturnsAsync(park);
        reports.Setup(repository => repository.CreateAsync(
                It.Is<HistoricalExistenceReport>(report =>
                    report.OwnerUserId == visit.UserId
                    && report.VisitId == visit.Id
                    && report.ParkName == park.Name
                    && report.ClaimedName == "Ancien Cyclone"),
                CancellationToken.None))
            .ReturnsAsync(HistoricalExistenceReportWriteOutcome.Success);
        SubmitHistoricalExistenceReportCommandHandler handler = new(
            visits.Object,
            parks.Object,
            reports.Object);

        ApplicationResult<HistoricalExistenceReportResult> result = await handler.HandleAsync(
            new SubmitHistoricalExistenceReportCommand(
                visit.UserId,
                visit.Id.Value,
                "Ancien Cyclone",
                "https://example.org/archive",
                null,
                "Près du lac"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Parc témoin", result.Value?.ParkName);
        Assert.Equal(HistoricalExistenceReportStatus.Pending, result.Value?.Status);
        visits.VerifyAll();
        parks.VerifyAll();
        reports.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotDiscloseWhetherAnotherUsersVisitExists()
    {
        Mock<IUserVisitRepository> visits = new(MockBehavior.Strict);
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        Mock<IHistoricalExistenceReportRepository> reports = new(MockBehavior.Strict);
        visits.Setup(repository => repository.GetOwnedAsync(
                VisitId.Parse("visit-1"),
                "owner-1",
                CancellationToken.None))
            .ReturnsAsync((Visit?)null);
        SubmitHistoricalExistenceReportCommandHandler handler = new(
            visits.Object,
            parks.Object,
            reports.Object);

        ApplicationResult<HistoricalExistenceReportResult> result = await handler.HandleAsync(
            new SubmitHistoricalExistenceReportCommand(
                "owner-1",
                "visit-1",
                "Ancien Cyclone",
                null,
                null,
                null));

        Assert.False(result.IsSuccess);
        Assert.Equal("visit.not-found", Assert.Single(result.Errors).Code);
        visits.VerifyAll();
        parks.VerifyNoOtherCalls();
        reports.VerifyNoOtherCalls();
    }

    private static Visit CreateVisit()
    {
        return Visit.Create(
            VisitId.Parse("visit-1"),
            "owner-1",
            "park-1",
            VisitDate.ForYear(1998, isApproximate: true),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            "Souvenir privé",
            "Ne doit jamais être copié dans le signalement",
            new DateTime(2026, 9, 27, 8, 0, 0, DateTimeKind.Utc));
    }
}
