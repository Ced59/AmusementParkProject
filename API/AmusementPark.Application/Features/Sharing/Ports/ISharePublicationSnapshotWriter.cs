using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface ISharePublicationSnapshotWriter
{
    SharePublicationType PublicationType { get; }

    Task<ApplicationResult<bool>> WriteAsync(
        SharePublicationSnapshotWriteRequest request,
        CancellationToken cancellationToken);
}
