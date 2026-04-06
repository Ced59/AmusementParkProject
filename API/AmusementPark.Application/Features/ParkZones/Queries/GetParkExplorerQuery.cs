using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkZones.Results;

namespace AmusementPark.Application.Features.ParkZones.Queries;

/// <summary>
/// Récupère l'explorateur d'un parc.
/// </summary>
/// <param name="ParkId">Identifiant du parc.</param>
/// <param name="IncludeHidden">Indique si les éléments non visibles doivent être inclus.</param>
public sealed record GetParkExplorerQuery(string ParkId, bool IncludeHidden = false) : IQuery<ApplicationResult<ParkExplorerResult>>;
