using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IPassportProfileShareSourceVersionProvider
{
    Task<ApplicationResult<PassportProfileShareSourceRevisionSnapshot>>
        PrepareOwnedSourceRevisionSnapshotAsync(
            string ownerUserId,
            CancellationToken cancellationToken);

    Task<ApplicationResult<PassportProfileShareSourceRevisionSnapshot>>
        GetOwnedSourceRevisionSnapshotAsync(
            string ownerUserId,
            CancellationToken cancellationToken);

    Task<ApplicationResult<PassportProfileShareSourceRevision>> ReconcileOwnedSourceVersionAsync(
        string ownerUserId,
        string sourceFingerprint,
        PassportProfileShareSourceRevisionSnapshot expectedSnapshot,
        CancellationToken cancellationToken);
}
