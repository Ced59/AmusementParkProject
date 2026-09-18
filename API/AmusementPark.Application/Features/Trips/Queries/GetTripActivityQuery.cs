using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Results;

namespace AmusementPark.Application.Features.Trips.Queries;

public sealed record GetTripActivityQuery(
    string UserId,
    string TripPlanId,
    long? BeforeSequence)
    : IQuery<ApplicationResult<TripActivityPageResult>>;
