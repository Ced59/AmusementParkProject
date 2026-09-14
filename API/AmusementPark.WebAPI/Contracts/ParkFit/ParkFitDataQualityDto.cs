namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ParkFitDataQualityDto
{
    public string ParkId { get; init; } = string.Empty;

    public string ParkName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int CoveragePercent { get; init; }

    public int VisibleAttractionCount { get; init; }

    public int AttractionWithConditionsCount { get; init; }

    public int DecisionEligibleAttractionCount { get; init; }

    public int ConditionCount { get; init; }

    public int DecisionEligibleConditionCount { get; init; }

    public int IssueItemCount { get; init; }

    public int MissingSourceItemCount { get; init; }

    public int MissingTimestampItemCount { get; init; }

    public int StaleEvidenceItemCount { get; init; }

    public int AmbiguousItemCount { get; init; }

    public DateTime? LastVerifiedAtUtc { get; init; }

    public IReadOnlyCollection<string> Issues { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<ParkFitDataQualityItemDto> IssueSamples { get; init; } =
        Array.Empty<ParkFitDataQualityItemDto>();

    public string RecommendationState { get; init; } = string.Empty;

    public long OperationalRevision { get; init; }

    public DateTime? OperationalUpdatedAtUtc { get; init; }

    public int PendingReportCount { get; init; }

    public IReadOnlyCollection<ParkFitOperationalDecisionDto> RecentDecisions { get; init; } =
        Array.Empty<ParkFitOperationalDecisionDto>();
}
