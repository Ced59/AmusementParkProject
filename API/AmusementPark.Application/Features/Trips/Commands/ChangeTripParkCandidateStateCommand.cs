using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record ChangeTripParkCandidateStateCommand(
    string UserId,
    string TripPlanId,
    long ExpectedPlanVersion,
    string CandidateId,
    long ExpectedCandidateVersion,
    TripParkCandidateState State)
    : ICommand<ApplicationResult<TripParkCandidateResult>>;
