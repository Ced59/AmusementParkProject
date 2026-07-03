using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Commands;

/// <summary>
/// Met à jour un parc existant.
/// </summary>
/// <param name="ParkId">Identifiant du parc.</param>
/// <param name="Park">État cible du parc.</param>
/// <param name="PreserveExistingStatus">Indique si le statut courant doit être conservé.</param>
public sealed record UpdateParkCommand(string ParkId, Park Park, bool PreserveExistingStatus = false) : ICommand<ApplicationResult<Park>>;
