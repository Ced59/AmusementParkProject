namespace AmusementPark.Core.Domain.History;

public enum HistoricalOperationalState
{
    KnownOpen = 0,
    KnownClosed = 1,
    PossiblyOpen = 2,
    Unknown = 3,
}
