using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.Watchlists;

public static class CollectionTargetStatusResolver
{
    public static CollectionTargetStatus Resolve(ParkStatus parkStatus)
    {
        return parkStatus switch
        {
            ParkStatus.Operating => CollectionTargetStatus.Available,
            ParkStatus.TemporarilyClosed => CollectionTargetStatus.TemporarilyClosed,
            ParkStatus.ClosedDefinitively or ParkStatus.Cancelled =>
                CollectionTargetStatus.PermanentlyClosed,
            _ => CollectionTargetStatus.Unknown,
        };
    }

    public static CollectionTargetStatus Resolve(ParkItem item, ParkStatus parentStatus)
    {
        ArgumentNullException.ThrowIfNull(item);
        CollectionTargetStatus parentCollectionStatus = Resolve(parentStatus);
        if (parentCollectionStatus == CollectionTargetStatus.PermanentlyClosed)
        {
            return parentCollectionStatus;
        }

        string? normalizedItemStatus = ParkItemStatusNormalizer.Normalize(
            item.AttractionDetails?.Status);
        if (normalizedItemStatus is ParkItemStatusNormalizer.ClosedDefinitively
            or ParkItemStatusNormalizer.Removed)
        {
            return CollectionTargetStatus.PermanentlyClosed;
        }

        if (parentCollectionStatus != CollectionTargetStatus.Available)
        {
            return parentCollectionStatus;
        }

        return normalizedItemStatus switch
        {
            ParkItemStatusNormalizer.Operating => CollectionTargetStatus.Available,
            ParkItemStatusNormalizer.TemporarilyClosed =>
                CollectionTargetStatus.TemporarilyClosed,
            ParkItemStatusNormalizer.UnderConstruction or ParkItemStatusNormalizer.Planned
                or ParkItemStatusNormalizer.Unknown => CollectionTargetStatus.Unknown,
            null when item.Category != ParkItemCategory.Attraction => parentCollectionStatus,
            _ => CollectionTargetStatus.Unknown,
        };
    }
}
