using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Queries;

/// <summary>
/// Requête de génération du sitemap seed public.
/// </summary>
/// <param name="SupportedLanguages">Langues réellement servies par le front.</param>
/// <param name="MaxDynamicUrlsPerType">Limite de sécurité par famille d'URLs dynamiques.</param>
public sealed record GetPublicSitemapSeedQuery(IReadOnlyCollection<string> SupportedLanguages, int MaxDynamicUrlsPerType)
    : IQuery<ApplicationResult<IReadOnlyCollection<PublicSitemapUrl>>>;
