namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportHistoricalCategoryStatistic(
    string Category,
    long CompletedRideCount,
    long DistinctAttractionCount);
