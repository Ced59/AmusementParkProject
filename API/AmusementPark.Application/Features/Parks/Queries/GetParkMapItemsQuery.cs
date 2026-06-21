using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Results;

namespace AmusementPark.Application.Features.Parks.Queries;

/// <summary>
/// Query publique optimisée pour la carte détaillée des éléments d'un parc.
/// </summary>
public sealed record GetParkMapItemsQuery(
    string ParkId,
    bool IncludeHidden = false,
    ClosedEntityFilter ClosedFilter = ClosedEntityFilter.OpenOnly) : IQuery<ApplicationResult<ParkMapItemsResult>>;
