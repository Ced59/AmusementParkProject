namespace AmusementPark.Application.Features.Seo.Models;

/// <summary>
/// Statistiques d'une section de sitemap.
/// </summary>
public sealed record SitemapSectionStats(string Key, string FileName, string DisplayName, int UrlCount, DateTime? LastModifiedUtc);
