namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportHistoricalStatistics(
    long VisitCount,
    int? FirstVisitYear,
    long CompletedRideCount,
    long CanonicallyResolvedRideCount,
    double CanonicalCoverageRate,
    long ParkCountAcrossMultipleEras,
    IReadOnlyCollection<PassportHistoricalParkEraStatistic> ParksAcrossEras,
    IReadOnlyCollection<PassportHistoricalDisappearedAttractionStatistic> DisappearedAttractions,
    IReadOnlyCollection<PassportHistoricalTransformationStatistic> Transformations,
    IReadOnlyCollection<PassportHistoricalNameStatistic> HistoricalNames,
    IReadOnlyCollection<PassportHistoricalCategoryStatistic> HistoricalCategories);
