using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.NearbyDestinations.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.NearbyDestinations.Queries;

public sealed record GetNearbyDestinationsQuery(NearbyDestinationKind SourceKind, string SourceId, int Limit = 4)
    : IQuery<ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>>>;
