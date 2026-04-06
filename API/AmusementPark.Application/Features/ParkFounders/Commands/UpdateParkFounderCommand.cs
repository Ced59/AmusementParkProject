using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFounders.Commands;

/// <summary>
/// Met à jour un park founder existant.
/// </summary>
/// <param name="Id">Identifiant de la ressource à mettre à jour.</param>
/// <param name="ParkFounder">État cible de la ressource.</param>
public sealed record UpdateParkFounderCommand(string Id, ParkFounder ParkFounder) : ICommand<ApplicationResult<ParkFounder>>;
