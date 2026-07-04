namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Champs de tri de la liste d'administration des parcs.
/// </summary>
public enum ParkAdminSortField
{
    Default = 0,
    Name = 1,
    ParkItemsTotalCount = 2,
    ParkItemsVisibleCount = 3,
    OpeningHoursStatus = 4,
    DataCompletenessScore = 5,
}

public enum ParkOpeningHoursAdminStatus
{
    NotConfigured = 0,
    Expired = 1,
    NeedsUpdate = 2,
    UpToDate = 3,
}

public enum ParkOpeningHoursAdminFilter
{
    All = 0,
    Configured = 1,
    NotConfigured = 2,
    UpToDate = 3,
    NeedsUpdate = 4,
    Expired = 5,
}
