using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOpeningHours.Commands;
using AmusementPark.Application.Features.ParkOpeningHours.Handlers;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkOpeningHours.Handlers;

public sealed class ParkOpeningHoursCommandHandlersTests
{
    [Fact]
    public async Task HandleAsync_WhenScheduleIsSaved_ShouldRefreshSitemap()
    {
        ParkOpeningHoursSchedule schedule = CreateSchedule();
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkOpeningHoursRepository> openingHoursRepository = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapRefreshScheduler> sitemapRefreshScheduler = new Mock<ISeoSitemapRefreshScheduler>(MockBehavior.Strict);
        Mock<IParkOpeningHoursFactualChangeCapture> factualChangeCapture =
            new Mock<IParkOpeningHoursFactualChangeCapture>(MockBehavior.Strict);

        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Park" });
        openingHoursRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkOpeningHoursSchedule?)null);
        openingHoursRepository
            .Setup(repository => repository.UpsertAsync(It.Is<ParkOpeningHoursSchedule>(candidate => candidate.ParkId == "park-1"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkOpeningHoursSchedule candidate, CancellationToken _) => candidate);
        factualChangeCapture
            .Setup(capture => capture.CaptureAsync(
                It.Is<Park>(park => park.Id == "park-1"),
                null,
                It.Is<ParkOpeningHoursSchedule>(candidate => candidate.ParkId == "park-1"),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        sitemapRefreshScheduler
            .Setup(scheduler => scheduler.RequestRefreshAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        UpsertParkOpeningHoursScheduleCommandHandler handler = new UpsertParkOpeningHoursScheduleCommandHandler(
            parkRepository.Object,
            openingHoursRepository.Object,
            new ParkOpeningHoursScheduleNormalizer(),
            new ParkOpeningHoursCoverageSegmentBuilder(),
            sitemapRefreshScheduler.Object,
            factualChangeCapture.Object);

        ApplicationResult<ParkOpeningHoursSchedule> result = await handler.HandleAsync(
            new UpsertParkOpeningHoursScheduleCommand(schedule),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        parkRepository.VerifyAll();
        openingHoursRepository.VerifyAll();
        sitemapRefreshScheduler.VerifyAll();
        factualChangeCapture.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenRequestIsCancelledAfterCommit_ShouldFinishDurableCapture()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync(
                "park-1",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Park" });
        Mock<IParkOpeningHoursRepository> openingHoursRepository =
            new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        openingHoursRepository
            .Setup(repository => repository.GetByParkIdAsync(
                "park-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkOpeningHoursSchedule?)null);
        openingHoursRepository
            .Setup(repository => repository.UpsertAsync(
                It.IsAny<ParkOpeningHoursSchedule>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => cancellation.Cancel())
            .ReturnsAsync((ParkOpeningHoursSchedule candidate, CancellationToken _) => candidate);
        Mock<IParkOpeningHoursFactualChangeCapture> factualChangeCapture =
            new Mock<IParkOpeningHoursFactualChangeCapture>(MockBehavior.Strict);
        factualChangeCapture
            .Setup(capture => capture.CaptureAsync(
                It.IsAny<Park>(),
                null,
                It.IsAny<ParkOpeningHoursSchedule>(),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        Mock<ISeoSitemapRefreshScheduler> sitemapRefreshScheduler =
            new Mock<ISeoSitemapRefreshScheduler>(MockBehavior.Strict);
        sitemapRefreshScheduler
            .Setup(scheduler => scheduler.RequestRefreshAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        UpsertParkOpeningHoursScheduleCommandHandler handler =
            new UpsertParkOpeningHoursScheduleCommandHandler(
                parkRepository.Object,
                openingHoursRepository.Object,
                new ParkOpeningHoursScheduleNormalizer(),
                new ParkOpeningHoursCoverageSegmentBuilder(),
                sitemapRefreshScheduler.Object,
                factualChangeCapture.Object);

        ApplicationResult<ParkOpeningHoursSchedule> result = await handler.HandleAsync(
            new UpsertParkOpeningHoursScheduleCommand(CreateSchedule()),
            cancellation.Token);

        Assert.True(result.IsSuccess);
        Assert.True(cancellation.IsCancellationRequested);
        factualChangeCapture.VerifyAll();
        openingHoursRepository.VerifyAll();
        parkRepository.VerifyAll();
        sitemapRefreshScheduler.VerifyAll();
    }

    [Theory]
    [InlineData(ParkStatus.Planned)]
    [InlineData(ParkStatus.UnderConstruction)]
    [InlineData(ParkStatus.TemporarilyClosed)]
    [InlineData(ParkStatus.ClosedDefinitively)]
    [InlineData(ParkStatus.Cancelled)]
    public async Task HandleAsync_WhenParkIsNotOperating_ShouldRejectSchedule(ParkStatus status)
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Lifecycle Park", Status = status });
        Mock<IParkOpeningHoursRepository> openingHoursRepository = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapRefreshScheduler> sitemapRefreshScheduler = new Mock<ISeoSitemapRefreshScheduler>(MockBehavior.Strict);
        Mock<IParkOpeningHoursFactualChangeCapture> factualChangeCapture =
            new Mock<IParkOpeningHoursFactualChangeCapture>(MockBehavior.Strict);
        UpsertParkOpeningHoursScheduleCommandHandler handler = new UpsertParkOpeningHoursScheduleCommandHandler(
            parkRepository.Object,
            openingHoursRepository.Object,
            new ParkOpeningHoursScheduleNormalizer(),
            new ParkOpeningHoursCoverageSegmentBuilder(),
            sitemapRefreshScheduler.Object,
            factualChangeCapture.Object);

        ApplicationResult<ParkOpeningHoursSchedule> result = await handler.HandleAsync(
            new UpsertParkOpeningHoursScheduleCommand(CreateSchedule()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "park-opening-hours.not-operating");
        openingHoursRepository.Verify(
            repository => repository.UpsertAsync(It.IsAny<ParkOpeningHoursSchedule>(), It.IsAny<CancellationToken>()),
            Times.Never);
        sitemapRefreshScheduler.Verify(
            scheduler => scheduler.RequestRefreshAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        factualChangeCapture.VerifyNoOtherCalls();
        parkRepository.VerifyAll();
    }

    private static ParkOpeningHoursSchedule CreateSchedule()
    {
        return new ParkOpeningHoursSchedule
        {
            ParkId = " park-1 ",
            TimeZoneId = "UTC",
            RegularRules = new List<ParkOpeningHoursRule>
            {
                new ParkOpeningHoursRule
                {
                    StartDate = new DateOnly(2026, 7, 1),
                    EndDate = new DateOnly(2026, 7, 31),
                    DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday },
                    TimeRanges = new List<ParkOpeningHoursTimeRange>
                    {
                        new ParkOpeningHoursTimeRange
                        {
                            OpensAt = new TimeOnly(10, 0),
                            ClosesAt = new TimeOnly(18, 0),
                        },
                    },
                },
            },
        };
    }
}
