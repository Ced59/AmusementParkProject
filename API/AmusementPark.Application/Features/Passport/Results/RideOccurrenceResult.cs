using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Results;

public sealed record RideOccurrenceResult(
    string Id,
    string VisitId,
    string ParkId,
    string ParkItemId,
    long SortPosition,
    RideOccurrenceMomentResult Moment,
    RideOccurrenceStatus Status,
    RideLogSource Source,
    HistoricalConsistency HistoricalConsistency,
    string? PrivateNote,
    bool CountsAsRide,
    long Version,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    RideOccurrenceTargetResult? Target = null,
    RideAssessmentResult? Assessment = null,
    bool HistoricalConflictConfirmed = false);
