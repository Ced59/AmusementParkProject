using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionManufacturers.Commands;

/// <summary>
/// Crée un nouveau attraction manufacturer.
/// </summary>
/// <param name="AttractionManufacturer">AttractionManufacturer à créer.</param>
public sealed record CreateAttractionManufacturerCommand(AttractionManufacturer AttractionManufacturer) : ICommand<ApplicationResult<AttractionManufacturer>>;
