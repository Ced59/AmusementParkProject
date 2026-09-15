using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Models;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Parks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkOpeningHours.Services;

public sealed class ParkOpeningHoursFactualChangeCaptureServiceTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Prepare_WithSourcedChangedSchedule_ShouldCreateHighConfidenceDraft()
    {
        Park park = new Park { Id = "park-1", Name = "Parc exemple" };
        ParkOpeningHoursSchedule previous = CreateSchedule(new TimeOnly(18, 0));
        ParkOpeningHoursSchedule current = CreateSchedule(new TimeOnly(19, 0));
        current.SourceUrl = "https://example.com/opening-hours";
        current.LastVerifiedAtUtc = RecordedAtUtc.AddHours(-1);
        ParkOpeningHoursFactualChangeCaptureService service = CreateService();

        ParkOpeningHoursFactualChangeDraft? draft = service.Prepare(
            park,
            previous,
            current);

        Assert.NotNull(draft);
        Assert.Equal(FactualEventType.OpeningCalendarChanged, draft.Type);
        Assert.Equal(ChangeTarget.ForPark("park-1"), draft.Target);
        Assert.NotEqual(draft.PreviousValue, draft.NewValue);
        Assert.Equal(DataConfidence.High, draft.Confidence);
        Assert.Equal(current.LastVerifiedAtUtc, draft.OccurredAtUtc);
        Assert.Equal("https://example.com/opening-hours", draft.Source.Url);
    }

    [Fact]
    public void Prepare_WhenCalendarIsRemoved_ShouldCreateChangeWithNullNewValue()
    {
        ParkOpeningHoursSchedule current = CreateSchedule(new TimeOnly(18, 0));
        current.RegularRules.Clear();
        current.SourceUrl = "https://example.com/opening-hours";
        current.LastVerifiedAtUtc = RecordedAtUtc;
        ParkOpeningHoursFactualChangeCaptureService service = CreateService();

        ParkOpeningHoursFactualChangeDraft? draft = service.Prepare(
            new Park { Id = "park-1", Name = "Parc exemple" },
            CreateSchedule(new TimeOnly(18, 0)),
            current);

        Assert.NotNull(draft);
        Assert.Equal(FactualEventType.OpeningCalendarChanged, draft.Type);
        Assert.NotNull(draft.PreviousValue);
        Assert.Null(draft.NewValue);
    }

    [Fact]
    public void Prepare_WithoutVerifiedSource_ShouldNotCreateDraft()
    {
        ParkOpeningHoursFactualChangeCaptureService service = CreateService();

        ParkOpeningHoursFactualChangeDraft? draft = service.Prepare(
            new Park { Id = "park-1", Name = "Parc exemple" },
            null,
            CreateSchedule(new TimeOnly(18, 0)));

        Assert.Null(draft);
    }

    [Fact]
    public async Task CaptureAsync_WhenOutboxRecordFails_ShouldKeepDurableSourceMarker()
    {
        ParkOpeningHoursPendingFactualChange pending = CreatePendingChange();
        Mock<IFactualChangeCaptureService> capture =
            new Mock<IFactualChangeCaptureService>(MockBehavior.Strict);
        capture.Setup(value => value.CapturePreparedAfterCommitAsync(
                pending.Entry,
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("outbox unavailable"));
        Mock<IParkOpeningHoursRepository> repository =
            new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        ParkOpeningHoursFactualChangeCaptureService service = new ParkOpeningHoursFactualChangeCaptureService(
            capture.Object,
            repository.Object,
            NullLogger<ParkOpeningHoursFactualChangeCaptureService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CaptureAsync(pending, CancellationToken.None));

        repository.Verify(
            value => value.MarkFactualChangeRecordedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        capture.VerifyAll();
    }

    [Fact]
    public async Task CaptureAsync_WhenOutboxIsDurable_ShouldClearSourceMarker()
    {
        ParkOpeningHoursPendingFactualChange pending = CreatePendingChange();
        Mock<IFactualChangeCaptureService> capture =
            new Mock<IFactualChangeCaptureService>(MockBehavior.Strict);
        capture.Setup(value => value.CapturePreparedAfterCommitAsync(
                pending.Entry,
                CancellationToken.None))
            .ReturnsAsync(new FactualChangeCaptureResult(
                FactualChangeCaptureDisposition.RecordedPendingScheduling,
                pending.Entry.Id,
                pending.Entry.EventId));
        Mock<IParkOpeningHoursRepository> repository =
            new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        repository.Setup(value => value.MarkFactualChangeRecordedAsync(
                "park-1",
                pending.Entry.Id,
                CancellationToken.None))
            .ReturnsAsync(true);
        ParkOpeningHoursFactualChangeCaptureService service = new ParkOpeningHoursFactualChangeCaptureService(
            capture.Object,
            repository.Object,
            NullLogger<ParkOpeningHoursFactualChangeCaptureService>.Instance);

        await service.CaptureAsync(pending, CancellationToken.None);

        capture.VerifyAll();
        repository.VerifyAll();
    }

    private static ParkOpeningHoursFactualChangeCaptureService CreateService()
    {
        return new ParkOpeningHoursFactualChangeCaptureService(
            Mock.Of<IFactualChangeCaptureService>(MockBehavior.Strict),
            Mock.Of<IParkOpeningHoursRepository>(MockBehavior.Strict),
            NullLogger<ParkOpeningHoursFactualChangeCaptureService>.Instance);
    }

    private static ParkOpeningHoursPendingFactualChange CreatePendingChange()
    {
        ParkOpeningHoursFactualChangeDraft draft = new ParkOpeningHoursFactualChangeDraft(
            FactualEventType.OpeningCalendarPublished,
            ChangeTarget.ForPark("park-1"),
            null,
            FactValue.FromText("calendar"),
            new SourceReference(
                SourceReferenceType.OfficialWebsite,
                "Park",
                "Calendar",
                "https://example.com/calendar",
                RecordedAtUtc),
            DataConfidence.High,
            RecordedAtUtc,
            "park:park-1:opening-calendar");
        FactualChangeOutboxEntry entry = FactualChangeOutboxEntry.Create(
            draft.ToCaptureRequest(1, RecordedAtUtc))!;
        return new ParkOpeningHoursPendingFactualChange("park-1", entry);
    }

    private static ParkOpeningHoursSchedule CreateSchedule(TimeOnly closesAt)
    {
        return new ParkOpeningHoursSchedule
        {
            ParkId = "park-1",
            TimeZoneId = "Europe/Paris",
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
                            ClosesAt = closesAt,
                        },
                    },
                },
            },
        };
    }
}
