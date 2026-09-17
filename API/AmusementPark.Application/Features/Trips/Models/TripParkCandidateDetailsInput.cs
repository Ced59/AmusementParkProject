namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripParkCandidateDetailsInput(
    IReadOnlyCollection<DateOnly> CandidateDates,
    string? CollectiveNote);
