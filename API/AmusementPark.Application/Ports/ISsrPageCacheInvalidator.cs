using System.Text.Json.Serialization;

namespace AmusementPark.Application.Ports;

public sealed class SsrPageCacheInvalidationRequest
{
    public const string RatingRankingPageGroup = "rating-rankings";

    [JsonPropertyName("all")]
    public bool All { get; init; }

    [JsonPropertyName("paths")]
    public IReadOnlyCollection<string> Paths { get; init; } = Array.Empty<string>();

    [JsonPropertyName("prefixes")]
    public IReadOnlyCollection<string> Prefixes { get; init; } = Array.Empty<string>();

    [JsonPropertyName("pageGroups")]
    public IReadOnlyCollection<string> PageGroups { get; init; } = Array.Empty<string>();

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

    public static SsrPageCacheInvalidationRequest RatingRankingPages()
    {
        return new SsrPageCacheInvalidationRequest
        {
            All = false,
            PageGroups = new string[] { RatingRankingPageGroup },
            IncludeSeoDocuments = false,
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
