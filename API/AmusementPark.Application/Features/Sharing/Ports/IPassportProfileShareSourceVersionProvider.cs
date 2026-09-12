using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IPassportProfileShareSourceVersionProvider
{
    Task<ApplicationResult<PassportProfileShareSourceRevisionSnapshot>>
        PrepareOwnedSourceRevisionSnapshotAsync(
            string ownerUserId,
            ShareContentPolicy contentPolicy,
            IReadOnlyCollection<string> selectedParkIds,
            CancellationToken cancellationToken);

    Task<ApplicationResult<PassportProfileShareSourceRevisionSnapshot>>
        GetOwnedSourceRevisionSnapshotAsync(
            string ownerUserId,
            ShareContentPolicy contentPolicy,
            IReadOnlyCollection<string> selectedParkIds,
            CancellationToken cancellationToken);

    Task<ApplicationResult<PassportProfileShareSourceRevision>> ReconcileOwnedSourceVersionAsync(
        string ownerUserId,
        string sourceFingerprint,
        PassportProfileShareSourceRevisionSnapshot expectedSnapshot,
        ShareContentPolicy contentPolicy,
        IReadOnlyCollection<string> selectedParkIds,
        CancellationToken cancellationToken);
}
