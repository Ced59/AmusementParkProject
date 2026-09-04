using AmusementPark.Application.Features.Passport.Ports;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class PassportContentMutationLeaseCompletion
{
    public static TResult Complete<TResult>(
        IVisitContentMutationLease? lease,
        TResult result)
    {
        lease?.MarkMutationCompleted();
        return result;
    }
}
