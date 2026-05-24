using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Queries;

/// <summary>
/// Récupère un élément de parc par identifiant.
/// </summary>
/// <param name="ParkItemId">Identifiant de l'élément.</param>
/// <param name="IncludeHidden">Indique si les éléments non visibles peuvent être retournés.</param>
public sealed record GetParkItemByIdQuery(string ParkItemId, bool IncludeHidden = false) : IQuery<ApplicationResult<ParkItem>>;
