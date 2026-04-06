using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Queries;

/// <summary>
/// Récupère un parc par identifiant.
/// </summary>
/// <param name="ParkId">Identifiant du parc.</param>
/// <param name="IncludeHidden">Indique si les parcs non visibles peuvent être retournés.</param>
public sealed record GetParkByIdQuery(string ParkId, bool IncludeHidden = false) : IQuery<ApplicationResult<Park>>;
