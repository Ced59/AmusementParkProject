namespace AmusementPark.Core.Domain.Sharing;

public sealed record ProfileComparisonMissedItemData(
    string Name,
    string Status,
    long? OccurrenceCount);
