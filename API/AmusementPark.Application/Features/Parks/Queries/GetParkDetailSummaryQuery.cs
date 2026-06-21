using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Results;

namespace AmusementPark.Application.Features.Parks.Queries;

/// <summary>
/// Récupère le résumé public optimisé d'un parc pour ParkDetailLight.
/// </summary>
/// <param name="ParkId">Identifiant du parc.</param>
/// <param name="IncludeHidden">Indique si les parcs non visibles peuvent être retournés.</param>
public sealed record GetParkDetailSummaryQuery(
    string ParkId,
    bool IncludeHidden = false,
    ClosedEntityFilter ClosedFilter = ClosedEntityFilter.OpenOnly) : IQuery<ApplicationResult<ParkDetailSummaryResult>>;
