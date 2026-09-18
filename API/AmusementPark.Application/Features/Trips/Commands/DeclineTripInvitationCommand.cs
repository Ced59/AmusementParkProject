using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Results;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record DeclineTripInvitationCommand(
    string UserId,
    string Token,
    string ClientOperationId)
    : ICommand<ApplicationResult<TripInvitationDecisionResult>>;
