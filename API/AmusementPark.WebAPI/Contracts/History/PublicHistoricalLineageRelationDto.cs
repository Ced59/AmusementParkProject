using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.History;

public sealed class PublicHistoricalLineageRelationDto
{
    public string SourceKey { get; init; } = string.Empty;

    public string TargetKey { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string Direction { get; init; } = string.Empty;

    public PublicHistoricalPeriodDto Period { get; init; } = new PublicHistoricalPeriodDto();

    public string EvidenceState { get; init; } = string.Empty;

    public IReadOnlyCollection<LocalizedTextDto> UncertaintyExplanations { get; init; } =
        Array.Empty<LocalizedTextDto>();

    public IReadOnlyCollection<PublicHistoricalSourceDto> Sources { get; init; } =
        Array.Empty<PublicHistoricalSourceDto>();
}
