namespace AmusementPark.Core.Domain.Parks;

public enum ParkOpeningHoursAdminFilter
{
    All = 0,
    Configured = 1,
    NotConfigured = 2,
    UpToDate = 3,
    NeedsUpdate = 4,
    Expired = 5,
}
