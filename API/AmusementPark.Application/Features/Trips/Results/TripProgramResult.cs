namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripProgramResult(
    IReadOnlyCollection<TripParkCandidateResult> Candidates,
    IReadOnlyCollection<TripDayPlanResult> Days);
