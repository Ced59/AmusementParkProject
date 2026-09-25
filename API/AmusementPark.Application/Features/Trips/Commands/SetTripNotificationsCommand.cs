using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Results;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record SetTripNotificationsCommand(
    string UserId,
    string TripPlanId,
    bool Enabled,
    long ExpectedVersion)
    : ICommand<ApplicationResult<TripNotificationStateResult>>;
