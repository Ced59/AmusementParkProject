using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Ports;

/// <summary>
/// Persistance des publications personnelles. La résolution publique exige toujours un jeton exact.
/// </summary>
public interface ISharePublicationRepository
{
    Task<SharePublication?> GetOwnedAsync(
        SharePublicationId publicationId,
        string ownerUserId,
        CancellationToken cancellationToken);

    Task<SharePublication?> GetOwnedBySourceAsync(
        string ownerUserId,
        SharePublicationType publicationType,
        string sourceScopeKey,
        CancellationToken cancellationToken);

    Task<SharePublication?> GetResolvableByTokenAsync(
        ShareToken shareToken,
        CancellationToken cancellationToken);

    Task<SharePublicationWriteOutcome> CreateAsync(
        SharePublication publication,
        CancellationToken cancellationToken);

    Task<SharePublicationWriteOutcome> ReplaceAsync(
        SharePublication publication,
        long expectedVersion,
        CancellationToken cancellationToken);
}
