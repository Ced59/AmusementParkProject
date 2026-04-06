using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOperators.Commands;

/// <summary>
/// Met à jour un park operator existant.
/// </summary>
/// <param name="Id">Identifiant de la ressource à mettre à jour.</param>
/// <param name="ParkOperator">État cible de la ressource.</param>
public sealed record UpdateParkOperatorCommand(string Id, ParkOperator ParkOperator) : ICommand<ApplicationResult<ParkOperator>>;
