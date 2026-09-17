using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record DeleteTripParkCandidateCommand(
    string UserId,
    string TripPlanId,
    long ExpectedPlanVersion,
    string CandidateId,
    long ExpectedCandidateVersion)
    : ICommand<ApplicationResult>;
