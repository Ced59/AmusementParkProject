namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportProfileParkStatistics(
    string ParkId,
    long VisitCount,
    int FirstVisitYear,
    int LastVisitYear,
    long CompletedRideCount,
    long RatedVisitCount,
    double? AverageVisitRating);
