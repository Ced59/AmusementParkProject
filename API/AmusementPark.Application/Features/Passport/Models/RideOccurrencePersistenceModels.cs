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

public sealed record RideOccurrenceCreationRequestItem(
    string ParkItemId,
    OccurrenceMoment Moment,
    RideOccurrenceStatus Status,
    RideLogSource Source,
    string? PrivateNote);

public sealed record RideOccurrenceCreationRequest(
    VisitId VisitId,
    string UserId,
    IReadOnlyList<RideOccurrenceCreationRequestItem> Items);

public enum IdempotentRideOccurrenceCreationStatus
{
    Created = 1,
    Replayed = 2,
    Conflict = 3,
}

public sealed record IdempotentRideOccurrenceCreationResult(
    IdempotentRideOccurrenceCreationStatus Status,
    IReadOnlyCollection<RideOccurrence> Occurrences);

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
}

public sealed record IdempotentRideOccurrenceReorderResult(
    IdempotentRideOccurrenceReorderStatus Status,
    RideOccurrence? Occurrence,
    bool WasNormalized);
