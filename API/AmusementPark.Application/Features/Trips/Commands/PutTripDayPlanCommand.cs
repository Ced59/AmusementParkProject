using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Results;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record PutTripDayPlanCommand(
    string UserId,
    string TripPlanId,
    long ExpectedPlanVersion,
    DateOnly LocalDate,
    long? ExpectedDayVersion,
    TripDayPlanInput Input)
    : ICommand<ApplicationResult<TripDayPlanResult>>;
