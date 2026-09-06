namespace AmusementPark.Application.Features.Sharing.Models;

public static class ShareSourceMutationCancellation
{
    public static CancellationTokenSource CreateLeaseSource(
        ShareSourceMutationLease? mutationLease)
    {
        return CreateLeaseSource(
            mutationLease is null
                ? Array.Empty<ShareSourceMutationLease>()
                : new[] { mutationLease });
    }

    public static CancellationTokenSource CreateLeaseSource(
        IEnumerable<ShareSourceMutationLease> mutationLeases)
    {
        return CreateLinkedSource(CancellationToken.None, mutationLeases);
    }

    public static CancellationTokenSource CreateLinkedSource(
        CancellationToken callerCancellationToken,
        ShareSourceMutationLease? mutationLease)
    {
        return CreateLinkedSource(
            callerCancellationToken,
            mutationLease is null
                ? Array.Empty<ShareSourceMutationLease>()
                : new[] { mutationLease });
    }

    public static CancellationTokenSource CreateLinkedSource(
        CancellationToken callerCancellationToken,
        IEnumerable<ShareSourceMutationLease> mutationLeases)
    {
        ArgumentNullException.ThrowIfNull(mutationLeases);
        CancellationToken[] cancellationTokens = mutationLeases
            .Select(static mutationLease => mutationLease.LeaseCancellationToken)
            .Where(static cancellationToken => cancellationToken.CanBeCanceled)
            .Prepend(callerCancellationToken)
            .ToArray();
        return CancellationTokenSource.CreateLinkedTokenSource(cancellationTokens);
    }
}
