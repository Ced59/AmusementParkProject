namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportHistoricalParkEraStatistic(
    string ParkId,
    int FirstVisitYear,
    int LastVisitYear,
    long VisitCount,
    long CanonicalEraCount);
