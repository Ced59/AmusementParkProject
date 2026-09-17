using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Results;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record UpdateTripParkCandidateCommand(
    string UserId,
    string TripPlanId,
    long ExpectedPlanVersion,
    string CandidateId,
    long ExpectedCandidateVersion,
    TripParkCandidateDetailsInput Input)
    : ICommand<ApplicationResult<TripParkCandidateResult>>;
