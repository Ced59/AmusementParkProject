namespace AmusementPark.WebAPI.Contracts.History;

public sealed class AdminHistoricalFactDto
{
    public string Id { get; init; } = string.Empty;

    public int Revision { get; init; }

    public AdminHistoricalSubjectDto Subject { get; init; } = new();

    public string Type { get; init; } = string.Empty;

    public AdminHistoricalPeriodDto Period { get; init; } = new();

    public string State { get; init; } = string.Empty;

    public string Importance { get; init; } = string.Empty;

    public string WorkflowState { get; init; } = string.Empty;

    public string PublicationState { get; init; } = string.Empty;

    public IReadOnlyCollection<AdminHistoricalLocalizedTextDto> PublicUncertaintyExplanation { get; init; } =
        Array.Empty<AdminHistoricalLocalizedTextDto>();

    public string? LifecycleBoundaryMeaning { get; init; }

    public string? AttributeKind { get; init; }

    public string? AttributeBoundaryMeaning { get; init; }

    public int? SequenceWithinDate { get; init; }

    public IReadOnlyCollection<AdminHistoricalEvidenceDto> Sources { get; init; } =
        Array.Empty<AdminHistoricalEvidenceDto>();

    public string? StructuredValue { get; init; }

    public string? OtherTypeLabel { get; init; }

    public string? NarrativeContentId { get; init; }
}
