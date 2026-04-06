using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkZones.Queries;

/// <summary>
/// Récupère les zones d'un parc.
/// </summary>
/// <param name="ParkId">Identifiant du parc.</param>
public sealed record GetParkZonesByParkIdQuery(string ParkId) : IQuery<ApplicationResult<IReadOnlyCollection<ParkZone>>>;
