using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Infrastructure.Services.Trips;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Trips;

public sealed class TripPreferenceCleanupReconciliationBackgroundServiceTests
{
    [Fact]
    public async Task ReconcileAsync_ShouldResolveTheScopedPreferenceCleanupReconciler()
    {
        Mock<ITripPreferenceRepository> repository = new(MockBehavior.Strict);
        repository.Setup(item => item.ReconcileDepartureCleanupAsync(
                TripPreferenceCleanupReconciliationBackgroundService.BatchSize,
                CancellationToken.None))
            .ReturnsAsync(0);
        ServiceCollection services = new();
        services.AddScoped(_ => repository.Object);
        services.AddScoped<TripPreferenceCleanupReconciler>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        TripPreferenceCleanupReconciliationBackgroundService service = new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<TripPreferenceCleanupReconciliationBackgroundService>.Instance,
            TimeProvider.System);

        await service.ReconcileAsync(CancellationToken.None);

        repository.VerifyAll();
    }
}
