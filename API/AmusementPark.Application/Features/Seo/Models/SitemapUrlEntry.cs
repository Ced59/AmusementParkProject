namespace AmusementPark.Application.Features.Seo.Models;

/// <summary>
/// URL publique candidate pour un sitemap.
/// </summary>
public sealed record SitemapUrlEntry(string RelativePath, DateTime? LastModifiedUtc = null, string? ChangeFrequency = null, decimal? Priority = null);
