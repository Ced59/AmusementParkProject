using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Results;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record CreateTripInvitationCommand(
    string UserId,
    string TripPlanId,
    string ClientOperationId,
    TripInvitationCreateInput Input)
    : ICommand<ApplicationResult<TripInvitationCreationResult>>;
