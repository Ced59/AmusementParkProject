using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IPassportProfileShareSourceRevisionGuard
{
    Task<ShareSourceMutationLease?> TryBeginMutationAsync(
        string ownerUserId,
        CancellationToken cancellationToken);

    Task CompleteMutationAsync(
        ShareSourceMutationLease? mutationLease,
        bool sourceChanged,
        CancellationToken cancellationToken);
}
