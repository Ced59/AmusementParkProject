using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Queries;

public sealed record GetRelatedParkItemsQuery(
    string ParkItemId,
    int Limit = 3,
    bool IncludeHidden = false,
    ClosedEntityFilter ClosedFilter = ClosedEntityFilter.OpenOnly)
    : IQuery<ApplicationResult<IReadOnlyCollection<ParkItem>>>;
