using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface ISharePublicationCacheInvalidationExecutor
{
    Task<bool> TryInvalidateAsync(
        SharePublicationCacheInvalidationRequest request,
        CancellationToken cancellationToken);
}
