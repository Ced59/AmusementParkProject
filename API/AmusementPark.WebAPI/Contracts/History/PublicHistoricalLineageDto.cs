namespace AmusementPark.WebAPI.Contracts.History;

public sealed class PublicHistoricalLineageDto
{
    public PublicHistoricalLineageSubjectDto Root { get; init; } = new PublicHistoricalLineageSubjectDto();

    public IReadOnlyCollection<PublicHistoricalLineageSubjectDto> Subjects { get; init; } =
        Array.Empty<PublicHistoricalLineageSubjectDto>();

    public IReadOnlyCollection<PublicHistoricalLineageRelationDto> Relations { get; init; } =
        Array.Empty<PublicHistoricalLineageRelationDto>();

    public bool HasDirectedCycle { get; init; }

    public bool IsTruncated { get; init; }

    public int MaximumDepth { get; init; }
}
