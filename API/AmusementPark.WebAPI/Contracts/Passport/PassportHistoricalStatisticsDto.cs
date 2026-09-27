namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportHistoricalStatisticsDto
{
    public long VisitCount { get; init; }
    public int? FirstVisitYear { get; init; }
    public long CompletedRideCount { get; init; }
    public long CanonicallyResolvedRideCount { get; init; }
    public double CanonicalCoverageRate { get; init; }
    public long ParkCountAcrossMultipleEras { get; init; }
    public IReadOnlyCollection<PassportHistoricalParkEraDto> ParksAcrossEras { get; init; } =
        Array.Empty<PassportHistoricalParkEraDto>();
    public IReadOnlyCollection<PassportHistoricalDisappearedAttractionDto>
        DisappearedAttractions { get; init; } =
            Array.Empty<PassportHistoricalDisappearedAttractionDto>();
    public IReadOnlyCollection<PassportHistoricalTransformationDto> Transformations { get; init; } =
        Array.Empty<PassportHistoricalTransformationDto>();
    public IReadOnlyCollection<PassportHistoricalNameDto> HistoricalNames { get; init; } =
        Array.Empty<PassportHistoricalNameDto>();
    public IReadOnlyCollection<PassportHistoricalCategoryDto> HistoricalCategories { get; init; } =
        Array.Empty<PassportHistoricalCategoryDto>();
}
