namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Audit métier de la capacité d'un parc à participer au moteur FIT.
/// </summary>
public sealed record ParkFitDataQualityAssessment
{
    public string ParkId { get; init; } = string.Empty;

    public string ParkName { get; init; } = string.Empty;

    public ParkFitDataQualityStatus Status { get; init; }

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

    public IReadOnlyCollection<ParkFitDataQualityIssue> Issues { get; init; } =
        Array.Empty<ParkFitDataQualityIssue>();

    public IReadOnlyCollection<ParkFitDataQualityItemAssessment> IssueSamples { get; init; } =
        Array.Empty<ParkFitDataQualityItemAssessment>();
}
