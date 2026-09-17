using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Results;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record SetTripPlanDatesCommand(
    string UserId,
    string TripPlanId,
    long ExpectedVersion,
    TripPlanDatesInput Input)
    : ICommand<ApplicationResult<TripPlanResult>>;
