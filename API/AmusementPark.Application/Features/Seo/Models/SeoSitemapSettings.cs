namespace AmusementPark.Application.Features.Seo.Models;

/// <summary>
/// Réglages SEO administrables liés aux sitemaps et à IndexNow.
/// </summary>
public sealed class SeoSitemapSettings
{
    public bool IsIndexNowEnabled { get; init; }

    public bool SubmitToIndexNowAfterManualGeneration { get; init; }

    public bool SubmitToIndexNowAfterAutomaticGeneration { get; init; }

    public string IndexNowKey { get; init; } = string.Empty;

    public string IndexNowKeyLocation { get; init; } = string.Empty;

    public IReadOnlyCollection<string> IndexNowEndpoints { get; init; } = Array.Empty<string>();

    public DateTime UpdatedAtUtc { get; init; } = DateTime.UtcNow;
}
