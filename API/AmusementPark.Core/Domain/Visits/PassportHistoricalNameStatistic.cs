namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportHistoricalNameStatistic(
    string ParkId,
    string ParkItemId,
    string NameAtVisit,
    string? CurrentName,
    int FirstVisitYear,
    int LastVisitYear,
    long CompletedRideCount);
