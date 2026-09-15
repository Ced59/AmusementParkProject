namespace AmusementPark.Core.Domain.Watchlists;

public enum UserCollectionWriteOutcome
{
    Success = 1,
    AlreadyExists = 2,
    ConcurrencyConflict = 3,
    LimitReached = 4,
}
