using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Commands;

/// <summary>
/// Crée un élément de parc.
/// </summary>
/// <param name="ParkItem">Élément à créer.</param>
public sealed record CreateParkItemCommand(ParkItem ParkItem) : ICommand<ApplicationResult<ParkItem>>;
