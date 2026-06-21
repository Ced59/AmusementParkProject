using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Results;

namespace AmusementPark.Application.Features.ParkItems.Queries;

public sealed record GetParkItemSiblingNavigationQuery(
    string ParkItemId,
    bool IncludeHidden = false,
    ClosedEntityFilter ClosedFilter = ClosedEntityFilter.OpenOnly)
    : IQuery<ApplicationResult<ParkItemSiblingNavigationResult>>;
