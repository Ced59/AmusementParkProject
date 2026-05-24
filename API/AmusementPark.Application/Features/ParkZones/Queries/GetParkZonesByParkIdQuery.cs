using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkZones.Queries;

/// <summary>
/// Récupère les zones d'un parc.
/// </summary>
/// <param name="ParkId">Identifiant du parc.</param>
/// <param name="IncludeHidden">Indique si les zones d'un parc non visible peuvent être retournées.</param>
public sealed record GetParkZonesByParkIdQuery(string ParkId, bool IncludeHidden = false) : IQuery<ApplicationResult<IReadOnlyCollection<ParkZone>>>;
