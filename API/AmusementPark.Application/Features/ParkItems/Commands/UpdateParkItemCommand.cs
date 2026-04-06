using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Commands;

/// <summary>
/// Met à jour un élément de parc.
/// </summary>
/// <param name="ParkItemId">Identifiant de l'élément.</param>
/// <param name="ParkItem">État cible de l'élément.</param>
public sealed record UpdateParkItemCommand(string ParkItemId, ParkItem ParkItem) : ICommand<ApplicationResult<ParkItem>>;
