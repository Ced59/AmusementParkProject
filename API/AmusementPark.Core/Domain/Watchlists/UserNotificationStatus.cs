namespace AmusementPark.Core.Domain.Watchlists;

public enum UserNotificationStatus
{
    Pending = 1,
    Delivered = 2,
    Read = 3,
    Dismissed = 4,
    Failed = 5,
    Suppressed = 6,
}
