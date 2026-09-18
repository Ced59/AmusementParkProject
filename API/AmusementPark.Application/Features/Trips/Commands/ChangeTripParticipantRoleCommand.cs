using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record ChangeTripParticipantRoleCommand(
    string UserId,
    string TripPlanId,
    string MemberId,
    TripDelegatedRole Role,
    long ExpectedVersion)
    : ICommand<ApplicationResult<TripParticipantListResult>>;
