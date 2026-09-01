using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Infrastructure.Services.Ratings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Ratings;

public sealed class RatingRankingRebuildReconciliationBackgroundServiceTests
{
    [Fact]
    public async Task StartAsync_ShouldRunAnImmediateBoundedReconciliation()
    {
        TaskCompletionSource<bool> reconciled = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IRatingRankingRebuildScheduler> scheduler =
            new Mock<IRatingRankingRebuildScheduler>(MockBehavior.Strict);
        Mock<IRatingRankingRecoveryCoordinator> recoveryCoordinator =
            new Mock<IRatingRankingRecoveryCoordinator>(MockBehavior.Strict);
        recoveryCoordinator
            .Setup(value => value.ReconcileRecoveredParkItemMutationsAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        scheduler
            .Setup(value => value.ScheduleOutstandingAsync(It.IsAny<CancellationToken>()))
            .Callback(() => reconciled.TrySetResult(true))
            .Returns(Task.CompletedTask);
        ServiceCollection services = new ServiceCollection();
        services.AddScoped(_ => recoveryCoordinator.Object);
        services.AddScoped(_ => scheduler.Object);
        using ServiceProvider provider = services.BuildServiceProvider();
        RatingRankingRebuildReconciliationBackgroundService service =
            new RatingRankingRebuildReconciliationBackgroundService(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<RatingRankingRebuildReconciliationBackgroundService>.Instance,
                TimeProvider.System);

        await service.StartAsync(CancellationToken.None);
        await reconciled.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await service.StopAsync(CancellationToken.None);

        scheduler.Verify(
            value => value.ScheduleOutstandingAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        recoveryCoordinator.Verify(
            value => value.ReconcileRecoveredParkItemMutationsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
