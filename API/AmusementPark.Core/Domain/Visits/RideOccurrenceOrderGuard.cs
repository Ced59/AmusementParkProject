namespace AmusementPark.Core.Domain.Visits;

public sealed record RideOccurrenceOrderGuard(
    RideOccurrenceId OccurrenceId,
    long SortPosition);
