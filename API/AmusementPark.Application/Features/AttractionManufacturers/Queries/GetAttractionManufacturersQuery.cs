using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Results;

namespace AmusementPark.Application.Features.AttractionManufacturers.Queries;

/// <summary>
/// Récupère la liste des attraction manufacturers.
/// </summary>
/// <param name="Paging">Pagination demandee.</param>
/// <param name="Search">Filtre de recherche.</param>
/// <param name="IncludeHidden">Inclut les constructeurs masques.</param>
public sealed record GetAttractionManufacturersQuery(PagedQuery Paging, string? Search = null, bool IncludeHidden = false)
    : IQuery<ApplicationResult<PagedResult<AttractionManufacturerResult>>>;
