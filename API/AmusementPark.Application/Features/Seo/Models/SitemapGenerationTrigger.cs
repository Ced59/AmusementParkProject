namespace AmusementPark.Application.Features.Seo.Models;

/// <summary>
/// Type de déclenchement d'une génération sitemap.
/// </summary>
public enum SitemapGenerationTrigger
{
    Manual = 0,
    Automatic = 1,
    PublicFallback = 2,
}
