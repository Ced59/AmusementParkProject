namespace AmusementPark.Core.Domain.Trips;

public enum TripDeletionState
{
    None = 1,
    Pending = 2,
    Purging = 3,
    Purged = 4,
}
