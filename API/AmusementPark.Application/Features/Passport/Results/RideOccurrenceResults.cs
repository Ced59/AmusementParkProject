using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Results;

public sealed record RideOccurrenceMomentResult(
    TimeOnly? LocalTime,
    bool IsApproximate);

public sealed record RideOccurrenceTargetResult(
    string Name,
    string? Category,
    string? LifecycleStatus,
    bool IsHistoricalSnapshot,
    DateOnly? OpeningDate = null,
    DateOnly? ClosingDate = null);

public sealed record RideAssessmentResult(
    double Value,
    string? PrivateComment,
    int Revision,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

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
    RideAssessmentResult? Assessment = null);

public sealed record CreateRideOccurrencesResult(
    IReadOnlyCollection<RideOccurrenceResult> Occurrences,
    bool WasReplayed,
    bool WasNormalized);

public sealed record RideOccurrencePageResult(
    IReadOnlyCollection<RideOccurrenceResult> Items,
    RideOccurrenceListCursor? NextCursor);

public sealed record ReorderRideOccurrenceResult(
    RideOccurrenceResult Occurrence,
    bool WasReplayed,
    bool WasNormalized);
