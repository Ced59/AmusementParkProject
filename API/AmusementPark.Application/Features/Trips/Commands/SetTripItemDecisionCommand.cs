using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Results;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record SetTripItemDecisionCommand(
    string UserId,
    string TripPlanId,
    long ExpectedPlanVersion,
    TripItemDecisionInput Decision)
    : ICommand<ApplicationResult<TripPreferenceSummaryResult>>;
