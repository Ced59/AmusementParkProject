using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.Parks.Contracts;

namespace AmusementPark.Application.Features.Parks.Ports;

/// <summary>
/// Port de stockage des documents de carte officielle, séparé du pipeline d'images.
/// </summary>
public interface IParkOfficialMapBinaryStorage
{
    Task SaveAsync(
        string storageKey,
        FilePayload file,
        string canonicalContentType,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken);

    Task<ParkOfficialMapBinaryMetadata?> GetMetadataAsync(
        string storageKey,
        CancellationToken cancellationToken);

    Task CopyToAsync(
        string storageKey,
        Stream destination,
        long offset,
        long? length,
        CancellationToken cancellationToken);

    Task<bool> CopyAsync(
        string sourceStorageKey,
        string targetStorageKey,
        CancellationToken cancellationToken);
}
