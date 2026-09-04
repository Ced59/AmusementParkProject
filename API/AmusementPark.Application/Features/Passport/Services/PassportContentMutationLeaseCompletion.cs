using AmusementPark.Application.Features.Passport.Ports;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class PassportContentMutationLeaseCompletion
{
    public static async Task<TResult> CompleteAsync<TResult>(
        this Task<TResult> operation,
        IVisitContentMutationLease? lease)
    {
        TResult result = await operation;
        lease?.MarkMutationCompleted();
        return result;
    }

    public static TResult Complete<TResult>(
        IVisitContentMutationLease? lease,
        TResult result)
    {
        lease?.MarkMutationCompleted();
        return result;
    }
}
