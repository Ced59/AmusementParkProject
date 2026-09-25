using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripExportCandidateResult(
    string? ParkName,
    bool IsParkAvailable,
    IReadOnlyCollection<DateOnly> CandidateDates,
    TripParkCandidateState State,
    string? CollectiveNote);
