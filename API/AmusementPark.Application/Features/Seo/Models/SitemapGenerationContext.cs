namespace AmusementPark.Application.Features.Seo.Models;

/// <summary>
/// Contexte de génération partagé par les providers sitemap.
/// </summary>
public sealed class SitemapGenerationContext
{
    public IReadOnlyCollection<string> SupportedLanguages { get; init; } = Array.Empty<string>();
}
