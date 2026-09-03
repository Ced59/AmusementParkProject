using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record RideOccurrenceListCursor(
    long SortPosition,
    DateTime CreatedAtUtc,
    RideOccurrenceId OccurrenceId);

public sealed record RideOccurrenceListCriteria(
    VisitId VisitId,
    string UserId,
    int Limit,
    RideOccurrenceListCursor? After = null)
{
    public const int DefaultLimit = 100;

    public const int MaximumLimit = 250;
}

public sealed record RideOccurrencePage(
    IReadOnlyCollection<RideOccurrence> Items,
    RideOccurrenceListCursor? NextCursor);

public sealed record RideOccurrenceAppendState(
    long? LastSortPosition,
    bool WasNormalizedForOperation);

public enum PendingPassportMutationKind
{
    Unknown = 0,
    Creation = 1,
    Reorder = 2,
    Delete = 3,
}

public sealed record PendingPassportMutationVisit(
    string UserId,
    VisitId VisitId,
    string OperationKeyHash,
    PendingPassportMutationKind Kind,
    RideOccurrenceCreationPreparation? CreationPreparation);

public sealed record RideOccurrenceCreationRequestItem(
    string ParkItemId,
    OccurrenceMoment Moment,
    RideOccurrenceStatus Status,
    RideLogSource Source,
    string? PrivateNote,
    bool ConfirmHistoricalConflict);

public sealed record RideOccurrenceCreationRequest(
    VisitId VisitId,
    string UserId,
    IReadOnlyList<RideOccurrenceCreationRequestItem> Items);

public sealed record RideOccurrenceCreationPreparation(
    string ParkId,
    VisitDate VisitDate,
    string? TimeZoneId,
    LocalServiceDayConvention ServiceDayConvention,
    IReadOnlyList<HistoricalConsistency> HistoricalConsistencies);

public enum RideOccurrenceCreationKeyReservationStatus
{
    Missing = 1,
    Reserved = 2,
    Replayed = 3,
    Finalized = 4,
    Conflict = 5,
}

public sealed record RideOccurrenceCreationKeyReservationResult(
    RideOccurrenceCreationKeyReservationStatus Status,
    RideOccurrenceCreationPreparation? Preparation = null);

public enum IdempotentRideOccurrenceCreationStatus
{
    Created = 1,
    Replayed = 2,
    Conflict = 3,
    ConcurrencyConflict = 4,
}

public sealed record IdempotentRideOccurrenceCreationResult(
    IdempotentRideOccurrenceCreationStatus Status,
    IReadOnlyCollection<RideOccurrence> Occurrences,
    bool WasNormalized = false);

public sealed record RideOccurrenceReorderRequest(
    VisitId VisitId,
    string UserId,
    RideOccurrenceId OccurrenceId,
    long ExpectedVersion,
    RideOccurrenceId? AnchorOccurrenceId,
    RideOccurrencePlacement Placement);

public sealed record RideOccurrenceVersionedChange(
    RideOccurrence Occurrence,
    long ExpectedVersion,
    long PreviousSortPosition);

public enum IdempotentRideOccurrenceReorderStatus
{
    Applied = 1,
    Replayed = 2,
    Conflict = 3,
    IdempotencyConflict = 4,
}

public sealed record IdempotentRideOccurrenceReorderResult(
    IdempotentRideOccurrenceReorderStatus Status,
    RideOccurrence? Occurrence,
    bool WasNormalized);
