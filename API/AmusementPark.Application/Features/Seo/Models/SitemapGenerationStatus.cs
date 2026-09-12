namespace AmusementPark.Application.Features.Seo.Models;

/// <summary>
/// État d'une génération sitemap.
/// </summary>
public enum SitemapGenerationStatus
{
    Idle = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Skipped = 4,
}
