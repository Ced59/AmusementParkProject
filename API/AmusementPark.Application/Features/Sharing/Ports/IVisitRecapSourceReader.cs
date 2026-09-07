using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IVisitRecapSourceReader
{
    Task<VisitRecapSourceRevision?> GetOwnedCompletedRevisionAsync(
        string ownerUserId,
        string visitId,
        CancellationToken cancellationToken);

    Task<VisitRecapSourceData?> GetOwnedCompletedAsync(
        string ownerUserId,
        string visitId,
        CancellationToken cancellationToken);
}
