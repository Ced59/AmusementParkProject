using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Results;

namespace AmusementPark.Application.Features.AttractionManufacturers.Queries;

/// <summary>
/// Récupère la liste des attraction manufacturers.
/// </summary>
public sealed record GetAttractionManufacturersQuery(PagedQuery Paging, string? Search = null)
    : IQuery<ApplicationResult<PagedResult<AttractionManufacturerResult>>>;
