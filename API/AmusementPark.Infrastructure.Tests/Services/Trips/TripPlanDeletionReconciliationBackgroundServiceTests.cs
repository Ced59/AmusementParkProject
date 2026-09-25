using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Services.Trips;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Trips;

public sealed class TripPlanDeletionReconciliationBackgroundServiceTests
{
    [Fact]
    public async Task ReconcileAsync_ShouldResolveTheScopedDeletionReconciler()
    {
        Mock<ITripPlanRepository> repository = new(MockBehavior.Strict);
        Mock<ITripNotificationSubscriptionRepository> notifications = new(MockBehavior.Strict);
        repository.Setup(item => item.ListPendingDeletionAsync(
                TripPlanDeletionReconciliationBackgroundService.BatchSize,
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripPlan>());
        ServiceCollection services = new();
        services.AddScoped(_ => repository.Object);
        services.AddScoped(_ => notifications.Object);
        services.AddScoped<TripPlanDeletionReconciler>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        TripPlanDeletionReconciliationBackgroundService service = new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<TripPlanDeletionReconciliationBackgroundService>.Instance,
            TimeProvider.System);

        await service.ReconcileAsync(CancellationToken.None);

        repository.VerifyAll();
    }
}
