using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Commands;

/// <summary>
/// Modifie la visibilité d'un parc.
/// </summary>
/// <param name="ParkId">Identifiant du parc.</param>
/// <param name="IsVisible">Nouvel état de visibilité.</param>
public sealed record UpdateParkVisibilityCommand(string ParkId, bool IsVisible) : ICommand<ApplicationResult<Park>>;
