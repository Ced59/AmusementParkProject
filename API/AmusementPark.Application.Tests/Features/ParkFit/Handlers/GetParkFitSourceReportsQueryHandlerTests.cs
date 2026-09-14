using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Handlers;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Core.Domain.ParkFit;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkFit.Handlers;

public sealed class GetParkFitSourceReportsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnAStablePagedProjection()
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
            "Horaires anciens",
            submittedAtUtc);
        ParkFitSourceReportSearchCriteria criteria = new ParkFitSourceReportSearchCriteria(
            new PagedQuery(2, 12),
            ParkFitSourceReportStatus.Pending);
        Mock<IParkFitSourceReportRepository> repository =
            new Mock<IParkFitSourceReportRepository>(MockBehavior.Strict);
        repository.Setup(value => value.SearchAsync(criteria, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ParkFitSourceReport>(new[] { report }, 2, 12, 13));
        GetParkFitSourceReportsQueryHandler handler =
            new GetParkFitSourceReportsQueryHandler(repository.Object);

        ApplicationResult<PagedResult<ParkFitSourceReportResult>> result =
            await handler.HandleAsync(new GetParkFitSourceReportsQuery(criteria));

        Assert.True(result.IsSuccess);
        Assert.Equal(13, result.Value!.TotalItems);
        ParkFitSourceReportResult item = Assert.Single(result.Value.Items);
        Assert.Equal("Parc témoin", item.ParkName);
        Assert.Equal("Horaires anciens", item.Details);
        repository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WithAnUnboundedPage_ShouldRejectWithoutReadingMongoDb()
    {
        Mock<IParkFitSourceReportRepository> repository =
            new Mock<IParkFitSourceReportRepository>(MockBehavior.Strict);
        GetParkFitSourceReportsQueryHandler handler =
            new GetParkFitSourceReportsQueryHandler(repository.Object);

        ApplicationResult<PagedResult<ParkFitSourceReportResult>> result =
            await handler.HandleAsync(new GetParkFitSourceReportsQuery(
                new ParkFitSourceReportSearchCriteria(new PagedQuery(1, 101))));

        Assert.False(result.IsSuccess);
        Assert.Equal("park-fit.reports.search.invalid", Assert.Single(result.Errors).Code);
        repository.VerifyNoOtherCalls();
    }
}
