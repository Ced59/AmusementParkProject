namespace AmusementPark.Core.Domain.History;

public enum HistoricalRelationType
{
    RenamedTo = 0,
    ReplacedBy = 1,
    MovedTo = 2,
    RethemedAs = 3,
    SuccessorOf = 4,
    SamePhysicalAssetAs = 5,
    SharesLocationWith = 6,
    OperatedByDuring = 7,
    LocatedInZoneDuring = 8,
}
