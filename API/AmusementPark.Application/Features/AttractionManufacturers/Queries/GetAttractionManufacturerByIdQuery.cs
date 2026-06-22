using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Results;

namespace AmusementPark.Application.Features.AttractionManufacturers.Queries;

/// <summary>
/// Récupère un attraction manufacturer par identifiant.
/// </summary>
/// <param name="Id">Identifiant métier.</param>
/// <param name="IncludeHidden">Inclut les constructeurs masques.</param>
public sealed record GetAttractionManufacturerByIdQuery(string Id, bool IncludeHidden = false) : IQuery<ApplicationResult<AttractionManufacturerResult>>;
