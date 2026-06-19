using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Queries;

public sealed record GetRelatedParkItemsQuery(string ParkItemId, int Limit = 3, bool IncludeHidden = false)
    : IQuery<ApplicationResult<IReadOnlyCollection<ParkItem>>>;
