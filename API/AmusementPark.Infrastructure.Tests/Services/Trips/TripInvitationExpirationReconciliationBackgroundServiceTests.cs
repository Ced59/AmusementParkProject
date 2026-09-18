using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Infrastructure.Services.Trips;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Trips;

public sealed class TripInvitationExpirationReconciliationBackgroundServiceTests
{
    [Fact]
    public async Task ReconcileAsync_ShouldResolveTheScopedInvitationReconciler()
    {
        Mock<ITripInvitationRepository> repository = new(MockBehavior.Strict);
        repository.Setup(item => item.ExpireElapsedAsync(
                TripInvitationExpirationReconciliationBackgroundService.BatchSize,
                CancellationToken.None))
            .ReturnsAsync(4);
        ServiceCollection services = new();
        services.AddScoped(_ => repository.Object);
        services.AddScoped<TripInvitationExpirationReconciler>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        TripInvitationExpirationReconciliationBackgroundService service = new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<TripInvitationExpirationReconciliationBackgroundService>.Instance,
            TimeProvider.System);

        await service.ReconcileAsync(CancellationToken.None);

        repository.VerifyAll();
    }
}
