namespace AmusementPark.Core.Domain.Sharing;

public sealed record ProfileComparisonMissedItemResult(
    string Name,
    string Status,
    long? CreatorOccurrenceCount,
    long? AcceptorOccurrenceCount);
