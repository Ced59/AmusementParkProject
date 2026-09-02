namespace AmusementPark.Application.Features.Ratings.Ports;

/// <summary>
/// Invalide les caches de lecture susceptibles de conserver un rang publié ou retiré.
/// L'implémentation appartient à la couche hôte, qui connaît les caches HTTP et SSR.
/// </summary>
public interface IRatingRankingPublicationCacheInvalidator
{
    Task<bool> InvalidateAsync(CancellationToken cancellationToken);
}
