using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record RevokeTripInvitationCommand(
    string UserId,
    string TripPlanId,
    string InvitationId,
    long ExpectedInvitationVersion,
    string ClientOperationId)
    : ICommand<ApplicationResult>;
