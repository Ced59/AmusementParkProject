namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Résumé actionnable d'une attraction dont les données FIT doivent être corrigées.
/// </summary>
public sealed record ParkFitDataQualityItemAssessment
{
    public string ParkItemId { get; init; } = string.Empty;

    public string ParkItemName { get; init; } = string.Empty;

    public int ConditionCount { get; init; }

    public int DecisionEligibleConditionCount { get; init; }

    public DateTime? LastVerifiedAtUtc { get; init; }

    public IReadOnlyCollection<ParkFitDataQualityIssue> Issues { get; init; } =
        Array.Empty<ParkFitDataQualityIssue>();
}
