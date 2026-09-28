namespace AmusementPark.WebAPI.Contracts.History;

public sealed class SaveHistoricalFactRequestDto
{
    public int? ExpectedRevision { get; init; }

    public string SubjectType { get; init; } = string.Empty;

    public string SubjectId { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public HistoricalPeriodRequestDto Period { get; init; } = new();

    public string State { get; init; } = string.Empty;

    public string Importance { get; init; } = string.Empty;

    public IReadOnlyCollection<HistoricalLocalizedTextRequestDto> PublicUncertaintyExplanation { get; init; } =
        Array.Empty<HistoricalLocalizedTextRequestDto>();

    public string? LifecycleBoundaryMeaning { get; init; }

    public string? AttributeKind { get; init; }

    public string? AttributeBoundaryMeaning { get; init; }

    public int? SequenceWithinDate { get; init; }

    public IReadOnlyCollection<HistoricalEvidenceSourceRequestDto> Sources { get; init; } =
        Array.Empty<HistoricalEvidenceSourceRequestDto>();

    public string? StructuredValue { get; init; }

    public string? OtherTypeLabel { get; init; }

    public string? NarrativeContentId { get; init; }

    public string? ReviewNote { get; init; }
}
