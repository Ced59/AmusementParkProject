using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IShareSourceRevisionRepository
{
    Task<ShareSourceMutationLease> BeginMutationAsync(
        string scopeKey,
        CancellationToken cancellationToken);

    Task<ShareSourceRevision> CompleteMutationAsync(
        ShareSourceMutationLease mutationLease,
        bool sourceChanged,
        CancellationToken cancellationToken);

    Task<ShareSourceRevision> GetOrCreateAsync(
        string scopeKey,
        CancellationToken cancellationToken);
}
