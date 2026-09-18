using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Services;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripInvitationExpirationReconcilerTests
{
    [Fact]
    public async Task ReconcileAsync_ShouldUseABoundedRepositoryBatch()
    {
        Mock<ITripInvitationRepository> repository = new(MockBehavior.Strict);
        repository.Setup(item => item.ExpireElapsedAsync(25, CancellationToken.None))
            .ReturnsAsync(3);
        TripInvitationExpirationReconciler reconciler = new(repository.Object);

        int expiredCount = await reconciler.ReconcileAsync(25, CancellationToken.None);

        Assert.Equal(3, expiredCount);
        repository.VerifyAll();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task ReconcileAsync_ShouldRejectUnboundedBatches(int limit)
    {
        TripInvitationExpirationReconciler reconciler = new(
            Mock.Of<ITripInvitationRepository>());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await reconciler.ReconcileAsync(limit, CancellationToken.None));
    }
}
