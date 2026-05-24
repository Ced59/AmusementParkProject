namespace AmusementPark.Application.Features.Seo.Models;

/// <summary>
/// URL publique à exposer dans le sitemap seed.
/// </summary>
/// <param name="RelativePath">Chemin public relatif commençant par '/'.</param>
/// <param name="LastModifiedUtc">Dernière modification UTC connue.</param>
public sealed record PublicSitemapUrl(string RelativePath, DateTime? LastModifiedUtc);
