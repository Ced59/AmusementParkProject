using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkOpeningHours.Services;

public sealed class ParkOpeningHoursCoverageNotificationProcessorTests
{
    private static readonly DateTime ReferenceUtcNow = new DateTime(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ProcessAsync_ShouldSkipNotConfiguredSchedulesAndNotifyOnlyThirtyAndZeroDayThresholds()
    {
        ParkOpeningHoursScheduleSummary notConfigured = new ParkOpeningHoursScheduleSummary
        {
            ParkId = "park-empty",
            TimeZoneId = "Europe/Paris",
            HasScheduleData = false,
            UpdatedAtUtc = ReferenceUtcNow,
        };
        ParkOpeningHoursScheduleSummary thirtyDays = CreateSummary("park-30", new DateOnly(2026, 6, 29), new DateOnly(2026, 7, 28));
        ParkOpeningHoursScheduleSummary expired = CreateSummary("park-expired", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 28));

        Mock<IParkOpeningHoursRepository> openingHoursRepository = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        openingHoursRepository
            .Setup(value => value.GetConfiguredSummariesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { notConfigured, thirtyDays, expired });
        openingHoursRepository
            .Setup(value => value.TryMarkCoverageNotificationSentAsync("park-30", 30, new DateOnly(2026, 6, 29), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        openingHoursRepository
            .Setup(value => value.TryMarkCoverageNotificationSentAsync("park-expired", 0, new DateOnly(2026, 6, 29), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-30", "park-expired" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Park { Id = "park-30", Name = "Thirty Park" },
                new Park { Id = "park-expired", Name = "Expired Park" },
            });

        List<ParkOpeningHoursCoverageNotification> notifications = new List<ParkOpeningHoursCoverageNotification>();
        Mock<IParkOpeningHoursNotificationService> notificationService = new Mock<IParkOpeningHoursNotificationService>(MockBehavior.Strict);
        notificationService
            .Setup(value => value.NotifyCoverageThresholdReachedAsync(It.IsAny<ParkOpeningHoursCoverageNotification>(), It.IsAny<CancellationToken>()))
            .Callback<ParkOpeningHoursCoverageNotification, CancellationToken>((notification, _) => notifications.Add(notification))
            .Returns(Task.CompletedTask);

        ParkOpeningHoursCoverageNotificationProcessor processor = new ParkOpeningHoursCoverageNotificationProcessor(
            openingHoursRepository.Object,
            parkRepository.Object,
            new ParkOpeningHoursAdminStatusResolver(),
            notificationService.Object);

        await processor.ProcessAsync(ReferenceUtcNow, CancellationToken.None);

        Assert.Equal(2, notifications.Count);
        Assert.Contains(notifications, notification => notification.ParkId == "park-30" && notification.ThresholdDays == 30 && notification.CompleteForDays == 30);
        Assert.Contains(notifications, notification => notification.ParkId == "park-expired" && notification.ThresholdDays == 0 && notification.CompleteForDays == 0);
        Assert.DoesNotContain(notifications, notification => notification.ParkId == "park-empty");
        openingHoursRepository.Verify(value => value.TryMarkCoverageNotificationSentAsync("park-empty", It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
        openingHoursRepository.VerifyAll();
        parkRepository.VerifyAll();
        notificationService.VerifyAll();
    }

    [Fact]
    public async Task ProcessAsync_WhenThresholdWasAlreadyMarked_ShouldNotSendNotification()
    {
        ParkOpeningHoursScheduleSummary thirtyDays = CreateSummary("park-30", new DateOnly(2026, 6, 29), new DateOnly(2026, 7, 28));

        Mock<IParkOpeningHoursRepository> openingHoursRepository = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        openingHoursRepository
            .Setup(value => value.GetConfiguredSummariesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { thirtyDays });
        openingHoursRepository
            .Setup(value => value.TryMarkCoverageNotificationSentAsync("park-30", 30, new DateOnly(2026, 6, 29), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-30" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Park { Id = "park-30", Name = "Thirty Park" } });

        Mock<IParkOpeningHoursNotificationService> notificationService = new Mock<IParkOpeningHoursNotificationService>(MockBehavior.Strict);

        ParkOpeningHoursCoverageNotificationProcessor processor = new ParkOpeningHoursCoverageNotificationProcessor(
            openingHoursRepository.Object,
            parkRepository.Object,
            new ParkOpeningHoursAdminStatusResolver(),
            notificationService.Object);

        await processor.ProcessAsync(ReferenceUtcNow, CancellationToken.None);

        notificationService.Verify(value => value.NotifyCoverageThresholdReachedAsync(It.IsAny<ParkOpeningHoursCoverageNotification>(), It.IsAny<CancellationToken>()), Times.Never);
        openingHoursRepository.VerifyAll();
        parkRepository.VerifyAll();
    }

    private static ParkOpeningHoursScheduleSummary CreateSummary(string parkId, DateOnly startDate, DateOnly endDate)
    {
        return new ParkOpeningHoursScheduleSummary
        {
            ParkId = parkId,
            TimeZoneId = "Europe/Paris",
            FirstDate = startDate,
            LastDate = endDate,
            HasScheduleData = true,
            UpdatedAtUtc = ReferenceUtcNow,
            CoverageSegments = new[]
            {
                new ParkOpeningHoursCoverageSegmentSummary
                {
                    StartDate = startDate,
                    EndDate = endDate,
                },
            },
        };
    }
}
