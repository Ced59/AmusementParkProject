using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IPassportProfileShareSnapshotRepository
{
    Task<bool> UpsertAsync(
        PassportProfileShareSnapshot snapshot,
        CancellationToken cancellationToken);

    Task<PassportProfileShareSnapshot?> GetAsync(
        SharePublicationId publicationId,
        long publicationVersion,
        CancellationToken cancellationToken);

    Task<bool> DeleteSupersededAsync(
        SharePublicationId publicationId,
        long publishedVersion,
        CancellationToken cancellationToken);
}
