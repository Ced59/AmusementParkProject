using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Results;

namespace AmusementPark.Application.Features.ParkItems.Queries;

/// <summary>
/// Récupère les éléments d'un parc.
/// </summary>
/// <param name="ParkId">Identifiant du parc.</param>
/// <param name="IncludeHidden">Indique si les éléments non visibles doivent être inclus.</param>
public sealed record GetParkItemsByParkIdQuery(
    string ParkId,
    bool IncludeHidden = true,
    ClosedEntityFilter ClosedFilter = ClosedEntityFilter.OpenOnly) : IQuery<ApplicationResult<IReadOnlyCollection<ParkItemListResult>>>;
