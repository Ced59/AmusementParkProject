using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Handlers;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Watchlists.Handlers;

public sealed class CaptureWatchPilotInteractionCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_StoresOnlyTheAggregateDayAndKindForACenterOpen()
    {
        Mock<IUserNotificationRepository> notifications = new(MockBehavior.Strict);
        Mock<IWatchPilotMetricsRepository> metrics = new(MockBehavior.Strict);
        metrics.Setup(value => value.IncrementInteractionAsync(
                new DateOnly(2026, 9, 17),
                WatchPilotInteractionKind.NotificationCenterOpened,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        CaptureWatchPilotInteractionCommandHandler handler = new(
            notifications.Object,
            CreateRecorder(metrics.Object),
            new FixedWatchPilotTimeProvider(
                new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero)));

        ApplicationResult result = await handler.HandleAsync(
            new CaptureWatchPilotInteractionCommand(
                "user-1",
                WatchPilotInteractionKind.NotificationCenterOpened,
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        notifications.VerifyNoOtherCalls();
        metrics.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_RejectsASourceOpenWithoutOwnedNotification()
    {
        Mock<IUserNotificationRepository> notifications = new(MockBehavior.Strict);
        Mock<IWatchPilotMetricsRepository> metrics = new(MockBehavior.Strict);
        CaptureWatchPilotInteractionCommandHandler handler = new(
            notifications.Object,
            CreateRecorder(metrics.Object));

        ApplicationResult result = await handler.HandleAsync(
            new CaptureWatchPilotInteractionCommand(
                "user-1",
                WatchPilotInteractionKind.SourceOpened,
                null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        notifications.VerifyNoOtherCalls();
        metrics.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_RejectsAServerOwnedUnsubscriptionMetric()
    {
        Mock<IUserNotificationRepository> notifications = new(MockBehavior.Strict);
        Mock<IWatchPilotMetricsRepository> metrics = new(MockBehavior.Strict);
        CaptureWatchPilotInteractionCommandHandler handler = new(
            notifications.Object,
            CreateRecorder(metrics.Object));

        ApplicationResult result = await handler.HandleAsync(
            new CaptureWatchPilotInteractionCommand(
                "user-1",
                WatchPilotInteractionKind.SubscriptionRemoved,
                null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        notifications.VerifyNoOtherCalls();
        metrics.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_FirstMisleadingReport_PersistsAndCountsOnce()
    {
        UserNotification notification = CreateNotification();
        Mock<IUserNotificationRepository> notifications = new(MockBehavior.Strict);
        notifications.Setup(value => value.GetOwnedAsync(
                "user-1",
                UserNotificationId.Parse("notification-1"),
                CancellationToken.None))
            .ReturnsAsync(notification);
        notifications.Setup(value => value.ReplaceAsync(
                It.Is<UserNotification>(candidate =>
                    candidate.MisleadingReportedAtUtc.HasValue && candidate.Version == 2),
                1,
                CancellationToken.None))
            .ReturnsAsync(UserNotificationWriteOutcome.Success);
        Mock<IWatchPilotMetricsRepository> metrics = new(MockBehavior.Strict);
        metrics.Setup(value => value.IncrementInteractionAsync(
                new DateOnly(2026, 9, 17),
                WatchPilotInteractionKind.MisleadingAlertReported,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        CaptureWatchPilotInteractionCommandHandler handler = new(
            notifications.Object,
            CreateRecorder(metrics.Object),
            new FixedWatchPilotTimeProvider(
                new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero)));

        ApplicationResult result = await handler.HandleAsync(
            new CaptureWatchPilotInteractionCommand(
                "user-1",
                WatchPilotInteractionKind.MisleadingAlertReported,
                "notification-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        notifications.VerifyAll();
        metrics.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_RepeatedMisleadingReport_DoesNotCountAgain()
    {
        UserNotification notification = CreateNotification();
        notification.ReportMisleading(new DateTime(2026, 9, 17, 11, 0, 0, DateTimeKind.Utc));
        Mock<IUserNotificationRepository> notifications = new(MockBehavior.Strict);
        notifications.Setup(value => value.GetOwnedAsync(
                "user-1",
                UserNotificationId.Parse("notification-1"),
                CancellationToken.None))
            .ReturnsAsync(notification);
        Mock<IWatchPilotMetricsRepository> metrics = new(MockBehavior.Strict);
        CaptureWatchPilotInteractionCommandHandler handler = new(
            notifications.Object,
            CreateRecorder(metrics.Object));

        ApplicationResult result = await handler.HandleAsync(
            new CaptureWatchPilotInteractionCommand(
                "user-1",
                WatchPilotInteractionKind.MisleadingAlertReported,
                "notification-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        notifications.VerifyAll();
        metrics.VerifyNoOtherCalls();
    }

    private static WatchPilotMetricsRecorder CreateRecorder(IWatchPilotMetricsRepository metrics)
    {
        return new WatchPilotMetricsRecorder(
            metrics,
            new Mock<ILogger<WatchPilotMetricsRecorder>>().Object,
            new FixedWatchPilotTimeProvider(
                new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero)));
    }

    private static UserNotification CreateNotification()
    {
        DateTime deliveredAtUtc = new(2026, 9, 17, 10, 0, 0, DateTimeKind.Utc);
        return UserNotification.Restore(
            UserNotificationId.Parse("notification-1"),
            "user-1",
            FactualChangeEventId.Parse("event-1"),
            WatchSubscriptionId.Parse("subscription-1"),
            FactualEventType.ParkNameChanged,
            FactualTargetType.Park,
            "park-1",
            "park-1",
            1,
            UserNotification.CurrentTemplateVersion,
            "FR",
            UserNotificationStatus.Delivered,
            deliveredAtUtc,
            deliveredAtUtc,
            null,
            null,
            deliveredAtUtc.AddDays(UserNotification.RetentionDays),
            1);
    }
}
