using AmusementPark.Application.Features.Passport.Ports;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class PassportLeaseCancellation
{
    public static CancellationTokenSource? Link(
        IVisitContentMutationLease? lease,
        CancellationToken requestCancellationToken)
    {
        if (lease is null || !lease.LeaseLostToken.CanBeCanceled)
        {
            return null;
        }

        return CancellationTokenSource.CreateLinkedTokenSource(
            requestCancellationToken,
            lease.LeaseLostToken);
    }
}
