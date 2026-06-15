namespace AmusementPark.WebAPI.OutputCaching;

/// <summary>
/// Domaines de cache public invalidables après une écriture administrateur.
/// </summary>
public enum PublicCacheScope
{
    /// <summary>Données publiques volatiles (parcs, items, zones, images…).</summary>
    Data,

    /// <summary>Données de référence (fondateurs, exploitants, fabricants, types…).</summary>
    ReferenceData,

    /// <summary>Documents SEO (robots.txt, sitemaps).</summary>
    Seo
}
