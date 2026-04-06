using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Results;

namespace AmusementPark.Application.Features.AttractionManufacturers.Queries;

/// <summary>
/// Récupère la liste des attraction manufacturers.
/// </summary>
public sealed record GetAttractionManufacturersQuery : IQuery<ApplicationResult<IReadOnlyCollection<AttractionManufacturerResult>>>;
