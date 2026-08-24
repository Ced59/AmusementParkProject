using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Search.Results;
using AmusementPark.Application.Features.Countries;

namespace AmusementPark.Application.Features.Search.Queries;

/// <summary>
/// Requête de recherche transversale.
/// </summary>
/// <param name="Text">Texte de recherche.</param>
/// <param name="Categories">Catégories filtrées éventuelles.</param>
/// <param name="Paging">Paramètres de pagination.</param>
/// <param name="LanguageCode">Langue d'affichage demandée.</param>
/// <param name="Region">Région du monde filtrée éventuelle.</param>
public sealed record SearchQuery(
    string Text,
    IReadOnlyCollection<string> Categories,
    PagedQuery Paging,
    string LanguageCode,
    WorldRegionFilter? Region = null) : IQuery<ApplicationResult<SearchResultPage<SearchHitResult>>>;
