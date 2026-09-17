using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Results;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record AddTripParkCandidateCommand(
    string UserId,
    string TripPlanId,
    long ExpectedPlanVersion,
    string IdempotencyKey,
    TripParkCandidateInput Input)
    : ICommand<ApplicationResult<CreateTripParkCandidateResult>>;
