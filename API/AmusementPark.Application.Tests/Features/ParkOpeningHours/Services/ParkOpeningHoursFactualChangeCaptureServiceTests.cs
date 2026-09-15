using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Parks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkOpeningHours.Services;

public sealed class ParkOpeningHoursFactualChangeCaptureServiceTests
{
    private static readonly DateTime UpdatedAtUtc =
        new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CaptureAsync_WithSourcedChangedSchedule_ShouldCaptureHighConfidenceFact()
    {
        Park park = new Park { Id = "park-1", Name = "Parc exemple" };
        ParkOpeningHoursSchedule previous = CreateSchedule(new TimeOnly(18, 0));
        ParkOpeningHoursSchedule current = CreateSchedule(new TimeOnly(19, 0));
        current.SourceUrl = "https://example.com/opening-hours";
        current.LastVerifiedAtUtc = UpdatedAtUtc.AddHours(-1);
        current.UpdatedAtUtc = UpdatedAtUtc;
        FactualChangeCaptureRequest? capturedRequest = null;
        Mock<IFactualChangeCaptureService> capture =
            new Mock<IFactualChangeCaptureService>(MockBehavior.Strict);
        capture.Setup(value => value.CaptureAfterCommitAsync(
                It.IsAny<FactualChangeCaptureRequest>(),
                CancellationToken.None))
            .Callback((FactualChangeCaptureRequest request, CancellationToken _) =>
                capturedRequest = request)
            .ReturnsAsync(new FactualChangeCaptureResult(
                FactualChangeCaptureDisposition.Scheduled,
                "outbox-1",
                "event-1"));
        ParkOpeningHoursFactualChangeCaptureService service =
            new ParkOpeningHoursFactualChangeCaptureService(
                capture.Object,
                NullLogger<ParkOpeningHoursFactualChangeCaptureService>.Instance);

        await service.CaptureAsync(park, previous, current, CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.Equal(FactualEventType.OpeningCalendarChanged, capturedRequest.Type);
        Assert.Equal(ChangeTarget.ForPark("park-1"), capturedRequest.Target);
        Assert.NotEqual(capturedRequest.PreviousValue, capturedRequest.NewValue);
        Assert.Equal(DataConfidence.High, capturedRequest.Confidence);
        Assert.Equal(current.LastVerifiedAtUtc, capturedRequest.OccurredAtUtc);
        Assert.Equal(current.UpdatedAtUtc.Ticks, capturedRequest.SourceRevision);
        Assert.Equal("https://example.com/opening-hours", capturedRequest.Source.Url);
        capture.VerifyAll();
    }

    [Fact]
    public async Task CaptureAsync_WithoutVerifiedSource_ShouldNotCaptureFact()
    {
        Mock<IFactualChangeCaptureService> capture =
            new Mock<IFactualChangeCaptureService>(MockBehavior.Strict);
        ParkOpeningHoursFactualChangeCaptureService service =
            new ParkOpeningHoursFactualChangeCaptureService(
                capture.Object,
                NullLogger<ParkOpeningHoursFactualChangeCaptureService>.Instance);

        await service.CaptureAsync(
            new Park { Id = "park-1", Name = "Parc exemple" },
            null,
            CreateSchedule(new TimeOnly(18, 0)),
            CancellationToken.None);

        capture.VerifyNoOtherCalls();
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
