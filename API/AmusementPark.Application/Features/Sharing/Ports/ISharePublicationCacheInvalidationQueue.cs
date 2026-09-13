using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Application.Features.Sharing.Ports;

/// <summary>
/// Programme la convergence des caches dérivés après une écriture autoritative.
/// Une indisponibilité de cette file ne doit jamais annuler une révocation ou une rotation persistée.
/// </summary>
public interface ISharePublicationCacheInvalidationQueue
{
    void Enqueue(SharePublicationCacheInvalidationRequest request);
}
