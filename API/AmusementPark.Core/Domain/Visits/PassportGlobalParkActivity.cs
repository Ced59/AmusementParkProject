namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportGlobalParkActivity(
    string ParkId,
    long VisitCount,
    long RecordedRideCount);
