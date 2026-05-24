using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkZones.Queries;

/// <summary>
/// Récupère une zone de parc par identifiant.
/// </summary>
/// <param name="ZoneId">Identifiant de la zone.</param>
/// <param name="IncludeHidden">Indique si une zone rattachée à un parc non visible peut être retournée.</param>
public sealed record GetParkZoneByIdQuery(string ZoneId, bool IncludeHidden = false) : IQuery<ApplicationResult<ParkZone>>;
