using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Infrastructure.Services.Passport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Passport;

public sealed class PassportAuditReconciliationBackgroundServiceTests
{
    [Fact]
    public async Task StartAsync_ShouldImmediatelyRunOneBoundedReconciliation()
    {
        TaskCompletionSource<bool> reconciled = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        bool pendingMutationsWereReconciled = false;
        bool provisionalCreationsWereReconciled = false;
        Mock<IPassportPendingMutationReconciler> pendingMutationReconciler =
            new Mock<IPassportPendingMutationReconciler>(MockBehavior.Strict);
        pendingMutationReconciler.Setup(value => value.ReconcileBatchAsync(
                50,
                It.IsAny<CancellationToken>()))
            .Callback(() => pendingMutationsWereReconciled = true)
            .ReturnsAsync(1);
        Mock<IRideOccurrenceRepository> occurrenceRepository =
            new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        occurrenceRepository.Setup(value => value.ReconcileProvisionalCreationAllocationsAsync(
                50,
                It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                Assert.True(pendingMutationsWereReconciled);
                provisionalCreationsWereReconciled = true;
            })
            .ReturnsAsync(2);
        Mock<IPassportAuditReconciler> reconciler =
            new Mock<IPassportAuditReconciler>(MockBehavior.Strict);
        reconciler.Setup(value => value.ReconcileBatchAsync(
                50,
                It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                Assert.True(provisionalCreationsWereReconciled);
                reconciled.TrySetResult(true);
            })
            .ReturnsAsync(3);
        ServiceCollection services = new ServiceCollection();
        services.AddScoped(_ => pendingMutationReconciler.Object);
        services.AddScoped(_ => occurrenceRepository.Object);
        services.AddScoped(_ => reconciler.Object);
        using ServiceProvider provider = services.BuildServiceProvider();
        PassportAuditReconciliationBackgroundService service =
            new PassportAuditReconciliationBackgroundService(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<PassportAuditReconciliationBackgroundService>.Instance,
                TimeProvider.System);

        await service.StartAsync(CancellationToken.None);
        await reconciled.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await service.StopAsync(CancellationToken.None);

        reconciler.Verify(
            value => value.ReconcileBatchAsync(50, It.IsAny<CancellationToken>()),
            Times.Once);
        pendingMutationReconciler.Verify(
            value => value.ReconcileBatchAsync(50, It.IsAny<CancellationToken>()),
            Times.Once);
        occurrenceRepository.Verify(
            value => value.ReconcileProvisionalCreationAllocationsAsync(
                50,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
