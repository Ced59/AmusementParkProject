using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.ParkZones.Commands;

/// <summary>
/// Supprime une zone de parc.
/// </summary>
/// <param name="ZoneId">Identifiant de la zone.</param>
public sealed record DeleteParkZoneCommand(string ZoneId) : ICommand<ApplicationResult>;
