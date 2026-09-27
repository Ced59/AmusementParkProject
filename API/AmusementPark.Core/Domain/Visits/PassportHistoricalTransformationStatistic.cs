namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportHistoricalTransformationStatistic(
    string ParkId,
    string ParkItemId,
    string CurrentName,
    string CurrentCategory,
    IReadOnlyCollection<string> NamesAtVisit,
    IReadOnlyCollection<string> CategoriesAtVisit,
    int FirstVisitYear,
    int LastVisitYear,
    long CompletedRideCount);
