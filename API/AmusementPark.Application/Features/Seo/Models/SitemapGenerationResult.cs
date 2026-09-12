namespace AmusementPark.Application.Features.Seo.Models;

/// <summary>
/// Résultat complet d'une génération sitemap.
/// </summary>
public sealed class SitemapGenerationResult
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public DateTime StartedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    public long DurationMs { get; init; }

    public SitemapGenerationStatus Status { get; init; }

    public SitemapGenerationTrigger Trigger { get; init; }

    public int TotalUrlCount { get; init; }

    public IReadOnlyCollection<SitemapSectionStats> Sections { get; init; } = Array.Empty<SitemapSectionStats>();

    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();

    public IndexNowSubmissionResult IndexNow { get; init; } = new IndexNowSubmissionResult();
}
