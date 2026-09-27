namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportHistoricalDisappearedAttractionStatistic(
    string ParkId,
    string ParkItemId,
    string NameAtVisit,
    int FirstVisitYear,
    int LastVisitYear,
    long CompletedRideCount);
