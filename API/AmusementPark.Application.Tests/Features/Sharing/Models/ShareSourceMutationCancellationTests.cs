using AmusementPark.Application.Features.Sharing.Models;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Models;

public sealed class ShareSourceMutationCancellationTests
{
    [Fact]
    public void CreateLinkedSource_WhenLeaseIsLost_ShouldCancelProtectedWriter()
    {
        using CancellationTokenSource leaseCancellation = new CancellationTokenSource();
        ShareSourceMutationLease mutationLease = ShareSourceMutationLease.Create(
            "personal-ranking:owner-1",
            leaseCancellation.Token);
        using CancellationTokenSource writerCancellation =
            ShareSourceMutationCancellation.CreateLinkedSource(
                CancellationToken.None,
                mutationLease);

        leaseCancellation.Cancel();

        Assert.True(writerCancellation.IsCancellationRequested);
    }

    [Fact]
    public void CreateLinkedSource_WhenCallerCancels_ShouldCancelProtectedWriter()
    {
        using CancellationTokenSource callerCancellation = new CancellationTokenSource();
        ShareSourceMutationLease mutationLease = ShareSourceMutationLease.Create(
            "personal-ranking:owner-1");
        using CancellationTokenSource writerCancellation =
            ShareSourceMutationCancellation.CreateLinkedSource(
                callerCancellation.Token,
                mutationLease);

        callerCancellation.Cancel();

        Assert.True(writerCancellation.IsCancellationRequested);
    }

    [Fact]
    public void CreateLeaseSource_WhenLeaseIsLost_ShouldCancelWithoutCallerToken()
    {
        using CancellationTokenSource leaseCancellation = new CancellationTokenSource();
        ShareSourceMutationLease mutationLease = ShareSourceMutationLease.Create(
            "personal-ranking:owner-1",
            leaseCancellation.Token);
        using CancellationTokenSource consistencyCancellation =
            ShareSourceMutationCancellation.CreateLeaseSource(mutationLease);

        leaseCancellation.Cancel();

        Assert.True(consistencyCancellation.IsCancellationRequested);
    }
}
