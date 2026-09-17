using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Application.Tests.Features.Watchlists.Handlers;
using AmusementPark.Core.Domain.Watchlists;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Watchlists;

public sealed class WatchSubscriptionLifecycleServiceTests
{
    [Fact]
    public async Task DeleteAsync_WhenSubscriptionIsDeleted_RecordsTheCanonicalUnsubscriptionMetric()
    {
        Mock<IWatchSubscriptionRepository> subscriptions = new(MockBehavior.Strict);
        subscriptions.Setup(value => value.DeleteAsync(
                "user-1",
                WatchSubscriptionId.Parse("subscription-1"),
                3,
                CancellationToken.None))
            .ReturnsAsync(WatchSubscriptionWriteOutcome.Success);
        Mock<IWatchPilotMetricsRepository> metrics = new(MockBehavior.Strict);
        metrics.Setup(value => value.IncrementInteractionAsync(
                new DateOnly(2026, 9, 17),
                WatchPilotInteractionKind.SubscriptionRemoved,
                1,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        WatchPilotMetricsRecorder recorder = new(
            metrics.Object,
            new Mock<ILogger<WatchPilotMetricsRecorder>>().Object,
            new FixedWatchPilotTimeProvider(
                new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero)));
        WatchSubscriptionLifecycleService service = new(
            subscriptions.Object,
            CreateTargetReader(),
            pilotMetricsRecorder: recorder);

        ApplicationResult result = await service.DeleteAsync(
            "user-1",
            "subscription-1",
            3,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        subscriptions.VerifyAll();
        metrics.VerifyAll();
    }

    private static UserCollectionTargetReader CreateTargetReader()
    {
        return new UserCollectionTargetReader(
            new Mock<IParkRepository>(MockBehavior.Strict).Object,
            new Mock<IParkItemRepository>(MockBehavior.Strict).Object,
            new Mock<IImageRepository>(MockBehavior.Strict).Object);
    }
}
