using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOperators.Queries;

/// <summary>
/// Récupère la liste des park operators.
/// </summary>
public sealed record GetParkOperatorsQuery : IQuery<ApplicationResult<IReadOnlyCollection<ParkOperator>>>;
