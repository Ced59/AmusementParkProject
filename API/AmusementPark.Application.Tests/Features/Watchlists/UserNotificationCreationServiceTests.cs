using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Application.Tests.Features.Watchlists.Handlers;
using AmusementPark.Core.Domain.Watchlists;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Watchlists;

public sealed class UserNotificationCreationServiceTests
{
    [Fact]
    public async Task CreateManyAsync_WhenRepositoryPreventsDuplicates_RecordsTheirExactCount()
    {
        Mock<IUserNotificationRepository> notifications = new(MockBehavior.Strict);
        notifications.Setup(value => value.CreateManyAsync(
                It.Is<IReadOnlyCollection<UserNotification>>(items => items.Count == 0),
                CancellationToken.None))
            .ReturnsAsync(new UserNotificationCreationResult(0, 2));
        Mock<IWatchPilotMetricsRepository> metrics = new(MockBehavior.Strict);
        metrics.Setup(value => value.IncrementInteractionAsync(
                new DateOnly(2026, 9, 17),
                WatchPilotInteractionKind.DuplicateDeliveryPrevented,
                2,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        WatchPilotMetricsRecorder recorder = new(
            metrics.Object,
            new Mock<ILogger<WatchPilotMetricsRecorder>>().Object,
            new FixedWatchPilotTimeProvider(
                new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero)));
        UserNotificationCreationService service = new(notifications.Object, recorder);

        UserNotificationCreationResult result = await service.CreateManyAsync(
            Array.Empty<UserNotification>(),
            CancellationToken.None);

        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(2, result.DuplicateCount);
        notifications.VerifyAll();
        metrics.VerifyAll();
    }
}
