using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Application.Tests.Features.Watchlists.Handlers;
using AmusementPark.Core.Domain.Watchlists;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Watchlists;

public sealed class WatchPilotMetricsRecorderTests
{
    [Fact]
    public async Task RecordBestEffortAsync_StoresTheAggregateForTheCurrentUtcDay()
    {
        Mock<IWatchPilotMetricsRepository> repository = new(MockBehavior.Strict);
        repository.Setup(value => value.IncrementInteractionAsync(
                new DateOnly(2026, 9, 17),
                WatchPilotInteractionKind.SubscriptionRemoved,
                1,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        WatchPilotMetricsRecorder recorder = new(
            repository.Object,
            new Mock<ILogger<WatchPilotMetricsRecorder>>().Object,
            new FixedWatchPilotTimeProvider(
                new DateTimeOffset(2026, 9, 17, 23, 30, 0, TimeSpan.Zero)));

        await recorder.RecordBestEffortAsync(
            WatchPilotInteractionKind.SubscriptionRemoved,
            CancellationToken.None);

        repository.VerifyAll();
    }

    [Fact]
    public async Task RecordBestEffortAsync_WhenStorageFails_DoesNotBreakTheBusinessAction()
    {
        Mock<IWatchPilotMetricsRepository> repository = new(MockBehavior.Strict);
        repository.Setup(value => value.IncrementInteractionAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<WatchPilotInteractionKind>(),
                It.IsAny<long>(),
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Metrics unavailable."));
        WatchPilotMetricsRecorder recorder = new(
            repository.Object,
            new Mock<ILogger<WatchPilotMetricsRecorder>>().Object);

        await recorder.RecordBestEffortAsync(
            WatchPilotInteractionKind.SubscriptionRemoved,
            CancellationToken.None);

        repository.VerifyAll();
    }
}
