using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record LeaveTripCommand(
    string UserId,
    string TripPlanId,
    long ExpectedVersion)
    : ICommand<ApplicationResult>;
