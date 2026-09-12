namespace AmusementPark.Core.Domain.Visits;

public sealed record RideOccurrenceOrderPosition(
    RideOccurrenceId OccurrenceId,
    long SortPosition);
