using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Services;

public sealed class PassportLeaseCancellationTests
{
    [Fact]
    public async Task Link_WhenLeaseOwnershipIsLost_ShouldCancelProtectedWork()
    {
        using CancellationTokenSource leaseLostSource = new CancellationTokenSource();
        Mock<IVisitContentMutationLease> lease = new Mock<IVisitContentMutationLease>();
        lease.SetupGet(value => value.LeaseLostToken).Returns(leaseLostSource.Token);
        using CancellationTokenSource? linked = PassportLeaseCancellation.Link(
            lease.Object,
            CancellationToken.None);
        Assert.NotNull(linked);

        await leaseLostSource.CancelAsync();

        Assert.True(linked.Token.IsCancellationRequested);
    }

    [Fact]
    public void Link_WhenLeaseCannotSignalLoss_ShouldKeepTheRequestTokenDirectly()
    {
        Mock<IVisitContentMutationLease> lease = new Mock<IVisitContentMutationLease>();
        lease.SetupGet(value => value.LeaseLostToken).Returns(CancellationToken.None);

        CancellationTokenSource? linked = PassportLeaseCancellation.Link(
            lease.Object,
            CancellationToken.None);

        Assert.Null(linked);
    }
}
