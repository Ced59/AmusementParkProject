namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportHistoricalStatisticsResult(
    long VisitCount,
    int? FirstVisitYear,
    long CompletedRideCount,
    long CanonicallyResolvedRideCount,
    double CanonicalCoverageRate,
    long ParkCountAcrossMultipleEras,
    IReadOnlyCollection<PassportHistoricalParkEraResult> ParksAcrossEras,
    IReadOnlyCollection<PassportHistoricalDisappearedAttractionResult> DisappearedAttractions,
    IReadOnlyCollection<PassportHistoricalTransformationResult> Transformations,
    IReadOnlyCollection<PassportHistoricalNameResult> HistoricalNames,
    IReadOnlyCollection<PassportHistoricalCategoryResult> HistoricalCategories);
