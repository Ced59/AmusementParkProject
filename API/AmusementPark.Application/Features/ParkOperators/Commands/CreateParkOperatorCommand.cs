using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOperators.Commands;

/// <summary>
/// Crée un nouveau park operator.
/// </summary>
/// <param name="ParkOperator">ParkOperator à créer.</param>
public sealed record CreateParkOperatorCommand(ParkOperator ParkOperator) : ICommand<ApplicationResult<ParkOperator>>;
