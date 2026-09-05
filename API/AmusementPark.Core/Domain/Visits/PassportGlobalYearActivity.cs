namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportGlobalYearActivity(
    int Year,
    long VisitCount,
    long RecordedRideCount);
