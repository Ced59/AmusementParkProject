using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IPassportProfileShareSourceRevisionGuard
{
    Task<IReadOnlyCollection<ShareSourceMutationLease>> TryBeginMutationAsync(
        string ownerUserId,
        IReadOnlyCollection<(string ParkId, int Year)> segments,
        CancellationToken cancellationToken);

    Task CompleteMutationAsync(
        IReadOnlyCollection<ShareSourceMutationLease> mutationLeases,
        bool sourceChanged,
        CancellationToken cancellationToken);
}
