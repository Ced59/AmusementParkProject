using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.HistoricalExistenceReports.Handlers;
using AmusementPark.Application.Features.HistoricalExistenceReports.Ports;
using AmusementPark.Application.Features.HistoricalExistenceReports.Queries;
using AmusementPark.Application.Features.HistoricalExistenceReports.Results;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.HistoricalExistenceReports.Handlers;

public sealed class ListOwnedHistoricalExistenceReportsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldScopeReportsToTheOwnedVisit()
    {
        Visit visit = CreateVisit();
        HistoricalExistenceReport report = HistoricalExistenceReport.Create(
            HistoricalExistenceReportId.New(),
            visit.UserId,
            visit.Id,
            visit.ParkId,
            "Parc témoin",
            visit.Date,
            "Ancien Cyclone",
            null,
            null,
            null,
            visit.CreatedAtUtc);
        Mock<IUserVisitRepository> visits = new(MockBehavior.Strict);
        Mock<IHistoricalExistenceReportRepository> reports = new(MockBehavior.Strict);
        visits.Setup(repository => repository.GetOwnedAsync(
                visit.Id,
                visit.UserId,
                CancellationToken.None))
            .ReturnsAsync(visit);
        reports.Setup(repository => repository.ListOwnedByVisitAsync(
                visit.UserId,
                visit.Id,
                CancellationToken.None))
            .ReturnsAsync(new[] { report });
        ListOwnedHistoricalExistenceReportsQueryHandler handler = new(
            visits.Object,
            reports.Object);

        ApplicationResult<IReadOnlyCollection<HistoricalExistenceReportResult>> result =
            await handler.HandleAsync(new ListOwnedHistoricalExistenceReportsQuery(
                visit.UserId,
                visit.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal("Ancien Cyclone", Assert.Single(result.Value!).ClaimedName);
        visits.VerifyAll();
        reports.VerifyAll();
    }

    private static Visit CreateVisit()
    {
        return Visit.Create(
            VisitId.Parse("visit-1"),
            "owner-1",
            "park-1",
            VisitDate.ForYear(1998),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            new DateTime(2026, 9, 27, 8, 0, 0, DateTimeKind.Utc));
    }
}
