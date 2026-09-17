using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record DeleteTripDayPlanCommand(
    string UserId,
    string TripPlanId,
    long ExpectedPlanVersion,
    DateOnly LocalDate,
    long ExpectedDayVersion)
    : ICommand<ApplicationResult>;
