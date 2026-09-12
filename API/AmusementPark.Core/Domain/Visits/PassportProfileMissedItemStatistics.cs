namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportProfileMissedItemStatistics(
    string Name,
    PassportProfileMissedItemStatus Status,
    long OccurrenceCount);
