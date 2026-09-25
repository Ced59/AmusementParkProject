using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Results;

namespace AmusementPark.Application.Features.Trips.Queries;

public sealed record GetTripPassportTransitionQuery(
    string UserId,
    string TripPlanId) : IQuery<ApplicationResult<TripPassportTransitionResult>>;
