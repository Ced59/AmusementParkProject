using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkZones.Queries;

/// <summary>
/// Récupère une zone de parc par identifiant.
/// </summary>
/// <param name="ZoneId">Identifiant de la zone.</param>
public sealed record GetParkZoneByIdQuery(string ZoneId) : IQuery<ApplicationResult<ParkZone>>;
