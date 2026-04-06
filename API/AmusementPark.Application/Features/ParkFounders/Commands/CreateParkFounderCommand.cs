using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFounders.Commands;

/// <summary>
/// Crée un nouveau park founder.
/// </summary>
/// <param name="ParkFounder">ParkFounder à créer.</param>
public sealed record CreateParkFounderCommand(ParkFounder ParkFounder) : ICommand<ApplicationResult<ParkFounder>>;
