using System;
using System.Collections.Generic;
using System.Linq;

namespace AmusementPark.WebAPI.OutputCaching;

/// <summary>
/// Marque un contrôleur (ou une action) dont les écritures réussies doivent
/// invalider les caches publics des domaines indiqués : OutputCache serveur et
/// cache de pages SSR. Le déclenchement effectif est assuré par
/// <see cref="InvalidatePublicCachesFilter"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class InvalidatesPublicCacheAttribute : Attribute
{
    public InvalidatesPublicCacheAttribute(params PublicCacheScope[] scopes)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        this.Scopes = scopes.Distinct().ToArray();
    }

    public IReadOnlyCollection<PublicCacheScope> Scopes { get; }

    /// <summary>
    /// Évince les tags OutputCache API en plus de l'invalidation SSR. Les imports
    /// massifs peuvent le désactiver pour éviter de refroidir tout le site.
    /// </summary>
    public bool EvictOutputCache { get; init; } = true;
}
