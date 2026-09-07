using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IVisitRecapShareSnapshotRepository
{
    Task<bool> UpsertAsync(
        VisitRecapShareSnapshot snapshot,
        CancellationToken cancellationToken);

    Task<VisitRecapShareSnapshot?> GetAsync(
        SharePublicationId publicationId,
        long publicationVersion,
        CancellationToken cancellationToken);

    Task<bool> DeleteSupersededAsync(
        SharePublicationId publicationId,
        long publishedVersion,
        CancellationToken cancellationToken);
}
