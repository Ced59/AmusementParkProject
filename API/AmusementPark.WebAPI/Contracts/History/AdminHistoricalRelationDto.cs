namespace AmusementPark.WebAPI.Contracts.History;

public sealed class AdminHistoricalRelationDto
{
    public string Id { get; init; } = string.Empty;

    public int Revision { get; init; }

    public AdminHistoricalSubjectDto Source { get; init; } = new();

    public AdminHistoricalSubjectDto Target { get; init; } = new();

    public string Type { get; init; } = string.Empty;

    public string Direction { get; init; } = string.Empty;

    public AdminHistoricalPeriodDto Period { get; init; } = new();

    public string State { get; init; } = string.Empty;

    public string WorkflowState { get; init; } = string.Empty;

    public string PublicationState { get; init; } = string.Empty;

    public IReadOnlyCollection<AdminHistoricalLocalizedTextDto> PublicUncertaintyExplanation { get; init; } =
        Array.Empty<AdminHistoricalLocalizedTextDto>();

    public IReadOnlyCollection<AdminHistoricalEvidenceDto> Sources { get; init; } =
        Array.Empty<AdminHistoricalEvidenceDto>();

    public string? EditorialNote { get; init; }
}
