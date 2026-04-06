using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkZones.Commands;

/// <summary>
/// Met à jour une zone de parc.
/// </summary>
/// <param name="ZoneId">Identifiant de la zone.</param>
/// <param name="Zone">État cible.</param>
public sealed record UpdateParkZoneCommand(string ZoneId, ParkZone Zone) : ICommand<ApplicationResult<ParkZone>>;
