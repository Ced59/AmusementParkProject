using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record DeleteTripPlanCommand(
    string UserId,
    string TripPlanId,
    long ExpectedVersion,
    DateTime? AuthenticationConfirmedAtUtc)
    : ICommand<ApplicationResult>;
