using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOpeningHours.Commands;
using AmusementPark.Application.Features.ParkOpeningHours.Handlers;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.ParkOpeningHours.Models;
using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.FactualEvents;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkOpeningHours.Handlers;

public sealed class ParkOpeningHoursCommandHandlersTests
{
    [Fact]
    public void Normalize_WhenVerificationDateIsFuture_ShouldReturnValidationFailure()
    {
        DateTimeOffset now = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider.Setup(value => value.GetUtcNow()).Returns(now);
        ParkOpeningHoursSchedule schedule = CreateSchedule();
        schedule.LastVerifiedAtUtc = now.AddMinutes(1).UtcDateTime;

        ApplicationResult<ParkOpeningHoursSchedule> result =
            new ParkOpeningHoursScheduleNormalizer(timeProvider.Object).Normalize(schedule);

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            static error => error.Code == "park-opening-hours.invalid");
        timeProvider.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenScheduleIsSaved_ShouldRefreshSitemap()
    {
        ParkOpeningHoursSchedule schedule = CreateSchedule();
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkOpeningHoursRepository> openingHoursRepository = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapRefreshScheduler> sitemapRefreshScheduler = new Mock<ISeoSitemapRefreshScheduler>(MockBehavior.Strict);
        Mock<IParkOpeningHoursFactualChangeCapture> factualChangeCapture =
            new Mock<IParkOpeningHoursFactualChangeCapture>(MockBehavior.Strict);
        ParkOpeningHoursFactualChangeDraft draft = CreateFactualDraft();
        ParkOpeningHoursPendingFactualChange pending = CreatePendingChange(draft);

        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Park" });
        openingHoursRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkOpeningHoursSchedule?)null);
        openingHoursRepository
            .Setup(repository => repository.UpsertWithFactualChangeAsync(
                It.Is<ParkOpeningHoursSchedule>(candidate => candidate.ParkId == "park-1"),
                draft,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkOpeningHoursSchedule candidate, ParkOpeningHoursFactualChangeDraft? _, CancellationToken _) =>
                new ParkOpeningHoursFactualWriteResult(candidate, pending));
        factualChangeCapture
            .Setup(capture => capture.Prepare(
                It.Is<Park>(park => park.Id == "park-1"),
                null,
                It.Is<ParkOpeningHoursSchedule>(candidate => candidate.ParkId == "park-1")))
            .Returns(draft);
        factualChangeCapture
            .Setup(capture => capture.CaptureAsync(pending, CancellationToken.None))
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
        ParkOpeningHoursFactualChangeDraft draft = CreateFactualDraft();
        ParkOpeningHoursPendingFactualChange pending = CreatePendingChange(draft);
        openingHoursRepository
            .Setup(repository => repository.GetByParkIdAsync(
                "park-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkOpeningHoursSchedule?)null);
        openingHoursRepository
            .Setup(repository => repository.UpsertWithFactualChangeAsync(
                It.IsAny<ParkOpeningHoursSchedule>(),
                draft,
                It.IsAny<CancellationToken>()))
            .Callback(() => cancellation.Cancel())
            .ReturnsAsync((ParkOpeningHoursSchedule candidate, ParkOpeningHoursFactualChangeDraft? _, CancellationToken _) =>
                new ParkOpeningHoursFactualWriteResult(candidate, pending));
        Mock<IParkOpeningHoursFactualChangeCapture> factualChangeCapture =
            new Mock<IParkOpeningHoursFactualChangeCapture>(MockBehavior.Strict);
        factualChangeCapture
            .Setup(capture => capture.Prepare(
                It.IsAny<Park>(),
                null,
                It.IsAny<ParkOpeningHoursSchedule>()))
            .Returns(draft);
        factualChangeCapture
            .Setup(capture => capture.CaptureAsync(pending, CancellationToken.None))
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
            repository => repository.UpsertWithFactualChangeAsync(
                It.IsAny<ParkOpeningHoursSchedule>(),
                It.IsAny<ParkOpeningHoursFactualChangeDraft?>(),
                It.IsAny<CancellationToken>()),
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

    private static ParkOpeningHoursFactualChangeDraft CreateFactualDraft()
    {
        DateTime occurredAtUtc = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
        return new ParkOpeningHoursFactualChangeDraft(
            FactualEventType.OpeningCalendarPublished,
            ChangeTarget.ForPark("park-1"),
            null,
            FactValue.FromText("calendar"),
            new SourceReference(
                SourceReferenceType.OfficialWebsite,
                "Park",
                "Calendar",
                "https://example.com/calendar",
                occurredAtUtc),
            DataConfidence.High,
            occurredAtUtc,
            "park:park-1:opening-calendar");
    }

    private static ParkOpeningHoursPendingFactualChange CreatePendingChange(
        ParkOpeningHoursFactualChangeDraft draft)
    {
        DateTime recordedAtUtc = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
        FactualChangeOutboxEntry entry = FactualChangeOutboxEntry.Create(
            draft.ToCaptureRequest(1, recordedAtUtc))!;
        return new ParkOpeningHoursPendingFactualChange("park-1", entry);
    }
}
