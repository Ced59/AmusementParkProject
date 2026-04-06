using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkZones.Commands;

/// <summary>
/// Crée une zone de parc.
/// </summary>
/// <param name="Zone">Zone à créer.</param>
public sealed record CreateParkZoneCommand(ParkZone Zone) : ICommand<ApplicationResult<ParkZone>>;
