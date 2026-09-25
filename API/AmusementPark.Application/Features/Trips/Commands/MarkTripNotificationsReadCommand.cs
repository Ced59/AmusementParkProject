using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Results;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record MarkTripNotificationsReadCommand(
    string UserId,
    string TripPlanId,
    long ExpectedVersion)
    : ICommand<ApplicationResult<TripNotificationStateResult>>;
