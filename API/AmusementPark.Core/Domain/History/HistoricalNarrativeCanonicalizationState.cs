namespace AmusementPark.Core.Domain.History;

public enum HistoricalNarrativeCanonicalizationState
{
    PendingReview = 0,
    Canonicalized = 1,
    [Obsolete("Use Canonicalized. This alias only keeps old persisted documents readable during the one-way migration.")]
    Migrated = Canonicalized,
    Blocked = 2,
}
