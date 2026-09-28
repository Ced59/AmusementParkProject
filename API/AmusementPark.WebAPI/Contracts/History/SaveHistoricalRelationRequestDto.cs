namespace AmusementPark.WebAPI.Contracts.History;

public sealed class SaveHistoricalRelationRequestDto
{
    public int? ExpectedRevision { get; init; }

    public string SourceSubjectType { get; init; } = string.Empty;

    public string SourceSubjectId { get; init; } = string.Empty;

    public string? SourceSubjectContextParkId { get; init; }

    public string TargetSubjectType { get; init; } = string.Empty;

    public string TargetSubjectId { get; init; } = string.Empty;

    public string? TargetSubjectContextParkId { get; init; }

    public string Type { get; init; } = string.Empty;

    public string Direction { get; init; } = string.Empty;

    public HistoricalPeriodRequestDto Period { get; init; } = new();

    public string State { get; init; } = string.Empty;

    public IReadOnlyCollection<HistoricalLocalizedTextRequestDto> PublicUncertaintyExplanation { get; init; } =
        Array.Empty<HistoricalLocalizedTextRequestDto>();

    public IReadOnlyCollection<HistoricalEvidenceSourceRequestDto> Sources { get; init; } =
        Array.Empty<HistoricalEvidenceSourceRequestDto>();

    public string? EditorialNote { get; init; }

    public string? ReviewNote { get; init; }
}
