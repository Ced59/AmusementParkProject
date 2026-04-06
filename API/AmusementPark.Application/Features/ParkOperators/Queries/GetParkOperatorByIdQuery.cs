using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOperators.Queries;

/// <summary>
/// Récupère un park operator par identifiant.
/// </summary>
/// <param name="Id">Identifiant métier.</param>
public sealed record GetParkOperatorByIdQuery(string Id) : IQuery<ApplicationResult<ParkOperator>>;
