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
        if (parentCollectionStatus is CollectionTargetStatus.PermanentlyClosed
            or CollectionTargetStatus.TemporarilyClosed)
        {
            return parentCollectionStatus;
        }

        string? normalizedItemStatus = ParkItemStatusNormalizer.Normalize(
            item.AttractionDetails?.Status);
        return normalizedItemStatus switch
        {
            ParkItemStatusNormalizer.Operating => CollectionTargetStatus.Available,
            ParkItemStatusNormalizer.TemporarilyClosed =>
                CollectionTargetStatus.TemporarilyClosed,
            ParkItemStatusNormalizer.ClosedDefinitively or ParkItemStatusNormalizer.Removed =>
                CollectionTargetStatus.PermanentlyClosed,
            _ => parentCollectionStatus,
        };
    }
}
