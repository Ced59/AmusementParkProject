using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IYearRecapShareSnapshotRepository
{
    Task<bool> UpsertAsync(
        YearRecapShareSnapshot snapshot,
        CancellationToken cancellationToken);

    Task<YearRecapShareSnapshot?> GetAsync(
        SharePublicationId publicationId,
        long publicationVersion,
        CancellationToken cancellationToken);

    Task<bool> DeleteSupersededAsync(
        SharePublicationId publicationId,
        long publishedVersion,
        CancellationToken cancellationToken);
}
