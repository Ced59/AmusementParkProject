using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.ParkItems.Commands;

/// <summary>
/// Supprime un élément de parc.
/// </summary>
/// <param name="ParkItemId">Identifiant de l'élément à supprimer.</param>
public sealed record DeleteParkItemCommand(string ParkItemId) : ICommand<ApplicationResult>;
