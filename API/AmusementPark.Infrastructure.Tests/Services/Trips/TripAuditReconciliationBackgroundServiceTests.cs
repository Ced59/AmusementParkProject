using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Infrastructure.Services.Trips;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Trips;

public sealed class TripAuditReconciliationBackgroundServiceTests
{
    [Fact]
    public async Task StartAsync_ShouldImmediatelyRunOneBoundedReconciliation()
    {
        TaskCompletionSource<bool> reconciled = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<ITripAuditReconciler> reconciler =
            new Mock<ITripAuditReconciler>(MockBehavior.Strict);
        reconciler.Setup(value => value.ReconcilePendingAsync(
                50,
                It.IsAny<CancellationToken>()))
            .Callback(() => reconciled.TrySetResult(true))
            .ReturnsAsync(3);
        ServiceCollection services = new ServiceCollection();
        services.AddScoped(_ => reconciler.Object);
        using ServiceProvider provider = services.BuildServiceProvider();
        TripAuditReconciliationBackgroundService service =
            new TripAuditReconciliationBackgroundService(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<TripAuditReconciliationBackgroundService>.Instance,
                TimeProvider.System);

        await service.StartAsync(CancellationToken.None);
        await reconciled.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await service.StopAsync(CancellationToken.None);

        reconciler.Verify(
            value => value.ReconcilePendingAsync(50, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
