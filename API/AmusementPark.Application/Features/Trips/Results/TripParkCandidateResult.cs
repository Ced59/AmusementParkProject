using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripParkCandidateResult(
    string CandidateId,
    string ParkId,
    string ParkName,
    IReadOnlyCollection<DateOnly> CandidateDates,
    TripParkCandidateSource Source,
    TripParkCandidateState State,
    string? CollectiveNote,
    TripFitRecommendationSnapshotResult? FitSnapshot,
    long SortPosition,
    long Version,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
