using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Explication structurée d'une condition évaluée ou inconnue.
/// </summary>
public sealed class AttractionCompatibilityReason
{
    public AttractionCompatibilityReason(
        AttractionCompatibilityReasonCode code,
        AttractionAccessCondition? condition = null,
        IReadOnlyCollection<AttractionAccessConditionEvidenceIssue>? evidenceIssues = null,
        IReadOnlyCollection<AttractionAccessConditionSemanticIssue>? semanticIssues = null)
    {
        this.Code = code;
        this.ConditionType = condition?.Type;
        this.RequiredValue = condition?.Value;
        this.MinimumCompanionAge = condition?.MinimumCompanionAge;
        this.Unit = condition?.Unit;
        this.Scope = condition?.Scope;
        this.ScopeDetail = Normalize(condition?.ScopeDetail);
        this.EvidenceIssues = evidenceIssues is null
            ? Array.Empty<AttractionAccessConditionEvidenceIssue>()
            : evidenceIssues.ToList();
        this.SemanticIssues = semanticIssues is null
            ? Array.Empty<AttractionAccessConditionSemanticIssue>()
            : semanticIssues.ToList();
    }

    public AttractionCompatibilityReasonCode Code { get; }

    public AttractionAccessConditionType? ConditionType { get; }

    public double? RequiredValue { get; }

    public int? MinimumCompanionAge { get; }

    public AttractionAccessConditionUnit? Unit { get; }

    public AttractionAccessConditionScope? Scope { get; }

    public string? ScopeDetail { get; }

    public IReadOnlyCollection<AttractionAccessConditionEvidenceIssue> EvidenceIssues { get; }

    public IReadOnlyCollection<AttractionAccessConditionSemanticIssue> SemanticIssues { get; }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
