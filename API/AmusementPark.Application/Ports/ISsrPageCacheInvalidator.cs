namespace AmusementPark.Application.Ports;

/// <summary>
/// Invalide le cache de pages rendues côté serveur (SSR) après une écriture de
/// contenu public. L'implémentation notifie le serveur SSR afin que les
/// modifications administrateur soient immédiatement visibles côté public,
/// sans attendre l'expiration naturelle du cache.
/// </summary>
public interface ISsrPageCacheInvalidator
{
    /// <summary>
    /// Demande au serveur SSR de purger l'intégralité de son cache de pages.
    /// L'opération ne doit jamais faire échouer l'écriture métier appelante.
    /// </summary>
    Task InvalidateAllAsync(CancellationToken cancellationToken = default);
}
