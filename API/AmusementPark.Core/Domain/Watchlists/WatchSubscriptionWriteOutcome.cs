namespace AmusementPark.Core.Domain.Watchlists;

public enum WatchSubscriptionWriteOutcome
{
    Success = 1,
    AlreadyExists = 2,
    LimitReached = 3,
    Conflict = 4,
    NotFound = 5,
}
