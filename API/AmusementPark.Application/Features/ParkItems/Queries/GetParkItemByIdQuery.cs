using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Queries;

/// <summary>
/// Récupère un élément de parc par identifiant.
/// </summary>
/// <param name="ParkItemId">Identifiant de l'élément.</param>
public sealed record GetParkItemByIdQuery(string ParkItemId) : IQuery<ApplicationResult<ParkItem>>;
