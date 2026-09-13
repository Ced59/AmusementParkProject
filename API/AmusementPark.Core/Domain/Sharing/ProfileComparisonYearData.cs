namespace AmusementPark.Core.Domain.Sharing;

public sealed record ProfileComparisonYearData(
    int Year,
    long VisitCount,
    long? RideCount);
