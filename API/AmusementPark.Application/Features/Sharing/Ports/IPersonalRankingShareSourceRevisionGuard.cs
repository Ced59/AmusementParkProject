using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IPersonalRankingShareSourceRevisionGuard
{
    Task<ShareSourceMutationLease?> BeginIdentityMutationAsync(
        string ownerUserId,
        PersonalRankingShareIdentityState before,
        PersonalRankingShareIdentityState after,
        CancellationToken cancellationToken);

    Task<ShareSourceMutationLease> BeginMutationAsync(
        string ownerUserId,
        CancellationToken cancellationToken);

    Task CompleteMutationAsync(
        ShareSourceMutationLease? mutationLease,
        bool sourceChanged,
        CancellationToken cancellationToken);
}
