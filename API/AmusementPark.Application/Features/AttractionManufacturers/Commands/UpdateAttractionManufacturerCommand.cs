using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionManufacturers.Commands;

/// <summary>
/// Met à jour un attraction manufacturer existant.
/// </summary>
/// <param name="Id">Identifiant de la ressource à mettre à jour.</param>
/// <param name="AttractionManufacturer">État cible de la ressource.</param>
public sealed record UpdateAttractionManufacturerCommand(string Id, AttractionManufacturer AttractionManufacturer) : ICommand<ApplicationResult<AttractionManufacturer>>;
