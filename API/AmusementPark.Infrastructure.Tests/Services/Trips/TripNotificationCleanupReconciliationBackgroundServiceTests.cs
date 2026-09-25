using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Infrastructure.Services.Trips;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Trips;

public sealed class TripNotificationCleanupReconciliationBackgroundServiceTests
{
    [Fact]
    public async Task ReconcileAsync_ShouldResolveTheScopedNotificationCleanupReconciler()
    {
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        Mock<ITripNotificationSubscriptionRepository> subscriptions = new(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.ListForCleanupAsync(
                null,
                TripNotificationCleanupReconciliationBackgroundService.BatchSize,
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<AmusementPark.Core.Domain.Trips.TripNotificationSubscription>());
        ServiceCollection services = new();
        services.AddScoped(_ => plans.Object);
        services.AddScoped(_ => subscriptions.Object);
        services.AddScoped<TripNotificationCleanupReconciler>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        TripNotificationCleanupReconciliationBackgroundService service = new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<TripNotificationCleanupReconciliationBackgroundService>.Instance,
            TimeProvider.System);

        await service.ReconcileAsync(CancellationToken.None);

        subscriptions.VerifyAll();
        plans.VerifyNoOtherCalls();
    }
}
