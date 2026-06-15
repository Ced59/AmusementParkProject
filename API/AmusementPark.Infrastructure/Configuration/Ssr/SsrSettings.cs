namespace AmusementPark.Infrastructure.Configuration.Ssr;

/// <summary>
/// Paramètres de communication serveur-à-serveur avec le serveur SSR (Node).
/// </summary>
public sealed class SsrSettings
{
    public const string SectionName = "Ssr";

    /// <summary>
    /// URL interne du serveur SSR (ex. http://amusementpark-front:4000).
    /// Vide = invalidation SSR désactivée.
    /// </summary>
    public string InternalBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Jeton partagé requis par l'endpoint interne d'invalidation du SSR.
    /// Vide = invalidation SSR désactivée.
    /// </summary>
    public string CacheInvalidationToken { get; set; } = string.Empty;
}
