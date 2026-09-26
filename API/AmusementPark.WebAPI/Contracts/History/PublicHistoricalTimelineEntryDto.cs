using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.History;

public sealed class PublicHistoricalTimelineEntryDto
{
    public string SubjectType { get; set; } = string.Empty;

    public string SubjectId { get; set; } = string.Empty;

    public string SubjectLabel { get; set; } = string.Empty;

    public string FactType { get; set; } = string.Empty;

    public PublicHistoricalPeriodDto Period { get; set; } = new PublicHistoricalPeriodDto();

    public string EvidenceState { get; set; } = string.Empty;

    public string Importance { get; set; } = string.Empty;

    public string? AttributeKind { get; set; }

    public string? PreviousDisplayValue { get; set; }

    public string? NextDisplayValue { get; set; }

    public string? OtherTypeLabel { get; set; }

    public IReadOnlyCollection<LocalizedTextDto> UncertaintyExplanations { get; set; } =
        Array.Empty<LocalizedTextDto>();

    public IReadOnlyCollection<PublicHistoricalSourceDto> Sources { get; set; } =
        Array.Empty<PublicHistoricalSourceDto>();
}
