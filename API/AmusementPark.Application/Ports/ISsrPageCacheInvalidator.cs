using System.Text.Json.Serialization;

namespace AmusementPark.Application.Ports;

public interface ISsrPageCacheInvalidator
{
    /// <summary>
    /// Demande une purge et indique si le serveur SSR l'a confirmée. Ce contrat
    /// strict est réservé aux publications qui doivent pouvoir être rejouées
    /// tant que les caches publics n'ont pas convergé.
    /// </summary>
    async Task<bool> TryInvalidateAsync(
        SsrPageCacheInvalidationRequest request,
        CancellationToken cancellationToken = default)
    {
        await this.InvalidateAsync(request, cancellationToken);
        return true;
    }

    /// <summary>
    /// Demande au serveur SSR de purger uniquement les pages impactees quand
    /// l'impact public peut etre resolu.
    /// </summary>
    Task InvalidateAsync(SsrPageCacheInvalidationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Demande au serveur SSR de purger l'intégralité de son cache de pages.
    /// L'opération ne doit jamais faire échouer l'écriture métier appelante.
    /// </summary>
    Task InvalidateAllAsync(CancellationToken cancellationToken = default);
}
