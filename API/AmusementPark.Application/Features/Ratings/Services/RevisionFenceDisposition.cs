namespace AmusementPark.Application.Features.Ratings.Services;

internal enum RevisionFenceDisposition
{
    Current,
    NewerRevisionExists,
    RequestedRevisionUnavailable,
    MutationPending,
    DependencyChanged,
}
