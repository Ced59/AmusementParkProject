namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportProfileYearStatistics(
    int Year,
    long VisitCount,
    long ParkCount,
    long CompletedRideCount);
