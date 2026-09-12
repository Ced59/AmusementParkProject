namespace AmusementPark.Application.Features.Seo.Models;

/// <summary>
/// Résultat de construction d'une section de sitemap.
/// </summary>
public sealed record SitemapSectionBuildResult(string Key, string FileName, string DisplayName, IReadOnlyCollection<SitemapUrlEntry> Urls);
