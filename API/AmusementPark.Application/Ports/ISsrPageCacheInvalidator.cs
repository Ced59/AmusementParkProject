using System.Text.Json.Serialization;

namespace AmusementPark.Application.Ports;

public sealed class SsrPageCacheInvalidationRequest
{
    [JsonPropertyName("all")]
    public bool All { get; init; }

    [JsonPropertyName("paths")]
    public IReadOnlyCollection<string> Paths { get; init; } = Array.Empty<string>();

    [JsonPropertyName("prefixes")]
    public IReadOnlyCollection<string> Prefixes { get; init; } = Array.Empty<string>();

    [JsonPropertyName("includeSeoDocuments")]
    public bool IncludeSeoDocuments { get; init; }

    [JsonPropertyName("allowStale")]
    public bool AllowStale { get; init; } = true;

    [JsonPropertyName("refresh")]
    public bool Refresh { get; init; } = true;

    public static SsrPageCacheInvalidationRequest AllCaches()
    {
        return new SsrPageCacheInvalidationRequest
        {
            All = true,
            IncludeSeoDocuments = true,
            AllowStale = false,
            Refresh = false,
        };
    }
}

/// <summary>
/// Invalide le cache de pages rendues côté serveur (SSR) après une écriture de
/// contenu public. L'implémentation notifie le serveur SSR afin que les
/// modifications administrateur soient immédiatement visibles côté public,
/// sans attendre l'expiration naturelle du cache.
/// </summary>
public interface ISsrPageCacheInvalidator
{
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
