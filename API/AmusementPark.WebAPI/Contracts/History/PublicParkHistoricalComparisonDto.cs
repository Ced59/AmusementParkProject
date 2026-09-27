namespace AmusementPark.WebAPI.Contracts.History;

public sealed record PublicParkHistoricalComparisonDto
{
    public string ParkId { get; init; } = string.Empty;

    public string ParkName { get; init; } = string.Empty;

    public PublicHistoricalDateDto FromInstant { get; init; } = new();

    public PublicHistoricalDateDto ToInstant { get; init; } = new();

    public IReadOnlyCollection<PublicHistoricalSubjectComparisonDto> Subjects { get; init; } =
        Array.Empty<PublicHistoricalSubjectComparisonDto>();

    public IReadOnlyCollection<PublicHistoricalCategoryNetChangeDto> CategoryNetChanges { get; init; } =
        Array.Empty<PublicHistoricalCategoryNetChangeDto>();

    public PublicHistoricalCoverageDto FromCoverage { get; init; } = new();

    public PublicHistoricalCoverageDto ToCoverage { get; init; } = new();

    public int FromUnclassifiedOpenItemCount { get; init; }

    public int ToUnclassifiedOpenItemCount { get; init; }

    public bool IsCategoryComparisonComplete { get; init; }

    public string MethodologyVersion { get; init; } = string.Empty;
}
