using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripParkCandidateInput(
    string ParkId,
    IReadOnlyCollection<DateOnly> CandidateDates,
    TripParkCandidateSource Source,
    string? CollectiveNote);
