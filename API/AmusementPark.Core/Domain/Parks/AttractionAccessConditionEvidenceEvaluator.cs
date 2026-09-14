using System.Globalization;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Évalue si la preuve d'une condition d'accès peut soutenir une décision.
/// </summary>
public static class AttractionAccessConditionEvidenceEvaluator
{
    public static IReadOnlyCollection<AttractionAccessConditionEvidenceIssue> Evaluate(
        AttractionAccessCondition condition,
        DateTime evaluatedAtUtc,
        TimeSpan maximumVerificationAge)
    {
        ArgumentNullException.ThrowIfNull(condition);

        if (evaluatedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The evaluation timestamp must use UTC.", nameof(evaluatedAtUtc));
        }

        if (maximumVerificationAge < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumVerificationAge),
                maximumVerificationAge,
                "The maximum verification age cannot be negative.");
        }

        List<AttractionAccessConditionEvidenceIssue> issues = new List<AttractionAccessConditionEvidenceIssue>();

        AddSchemaIssues(condition, issues);
        AddSourceIssues(condition, issues);
        AddTimestampIssues(condition, evaluatedAtUtc, maximumVerificationAge, issues);
        AddScopeIssues(condition, issues);

        return issues;
    }

    public static bool IsDecisionEligible(
        AttractionAccessCondition condition,
        DateTime evaluatedAtUtc,
        TimeSpan maximumVerificationAge)
    {
        return Evaluate(condition, evaluatedAtUtc, maximumVerificationAge).Count == 0;
    }

    private static void AddSchemaIssues(
        AttractionAccessCondition condition,
        ICollection<AttractionAccessConditionEvidenceIssue> issues)
    {
        if (condition.ProvenanceSchemaVersion != AttractionAccessCondition.CurrentProvenanceSchemaVersion)
        {
            issues.Add(AttractionAccessConditionEvidenceIssue.UnsupportedSchemaVersion);
        }

        if (condition.EffectiveFrom.HasValue
            && condition.EffectiveTo.HasValue
            && condition.EffectiveTo.Value < condition.EffectiveFrom.Value)
        {
            issues.Add(AttractionAccessConditionEvidenceIssue.InvalidEffectivePeriod);
        }
    }

    private static void AddSourceIssues(
        AttractionAccessCondition condition,
        ICollection<AttractionAccessConditionEvidenceIssue> issues)
    {
        if (condition.SourceKind is not AttractionAccessConditionSourceKind.Official
            and not AttractionAccessConditionSourceKind.OperatorProvided)
        {
            issues.Add(AttractionAccessConditionEvidenceIssue.SourceNotDecisionEligible);
        }

        bool hasSourceReference = !string.IsNullOrWhiteSpace(condition.SourceReference);
        bool hasSourceUrl = !string.IsNullOrWhiteSpace(condition.SourceUrl);
        if (!hasSourceReference && !hasSourceUrl)
        {
            issues.Add(AttractionAccessConditionEvidenceIssue.MissingSourceReference);
        }
        else if (hasSourceUrl && !DecisionEvidenceUrlValidator.IsValid(condition.SourceUrl))
        {
            issues.Add(AttractionAccessConditionEvidenceIssue.InvalidSourceUrl);
        }

        if (string.IsNullOrWhiteSpace(condition.SourceLanguageCode))
        {
            issues.Add(AttractionAccessConditionEvidenceIssue.MissingSourceLanguage);
        }
        else if (!IsValidLanguageCode(condition.SourceLanguageCode))
        {
            issues.Add(AttractionAccessConditionEvidenceIssue.InvalidSourceLanguage);
        }

        if (!condition.SourceSummary.Any(static text => !string.IsNullOrWhiteSpace(text.Value)))
        {
            issues.Add(AttractionAccessConditionEvidenceIssue.MissingSourceSummary);
        }

        if (condition.SourceConfidence is not AttractionAccessConditionConfidence.Medium
            and not AttractionAccessConditionConfidence.High)
        {
            issues.Add(AttractionAccessConditionEvidenceIssue.InsufficientConfidence);
        }
    }

    private static void AddTimestampIssues(
        AttractionAccessCondition condition,
        DateTime evaluatedAtUtc,
        TimeSpan maximumVerificationAge,
        ICollection<AttractionAccessConditionEvidenceIssue> issues)
    {
        if (!condition.CollectedAtUtc.HasValue)
        {
            issues.Add(AttractionAccessConditionEvidenceIssue.MissingCollectionTimestamp);
        }

        if (!condition.VerifiedAtUtc.HasValue)
        {
            issues.Add(AttractionAccessConditionEvidenceIssue.MissingVerificationTimestamp);

            if (condition.CollectedAtUtc.HasValue
                && condition.CollectedAtUtc.Value.Kind != DateTimeKind.Utc)
            {
                issues.Add(AttractionAccessConditionEvidenceIssue.TimestampNotUtc);
            }

            return;
        }

        if (condition.VerifiedAtUtc.Value.Kind != DateTimeKind.Utc
            || (condition.CollectedAtUtc.HasValue && condition.CollectedAtUtc.Value.Kind != DateTimeKind.Utc))
        {
            issues.Add(AttractionAccessConditionEvidenceIssue.TimestampNotUtc);
        }

        if (condition.CollectedAtUtc.HasValue
            && condition.VerifiedAtUtc.Value < condition.CollectedAtUtc.Value)
        {
            issues.Add(AttractionAccessConditionEvidenceIssue.VerificationBeforeCollection);
        }

        if (condition.VerifiedAtUtc.Value > evaluatedAtUtc)
        {
            issues.Add(AttractionAccessConditionEvidenceIssue.VerificationInFuture);
        }
        else if (evaluatedAtUtc - condition.VerifiedAtUtc.Value > maximumVerificationAge)
        {
            issues.Add(AttractionAccessConditionEvidenceIssue.VerificationStale);
        }
    }

    private static void AddScopeIssues(
        AttractionAccessCondition condition,
        ICollection<AttractionAccessConditionEvidenceIssue> issues)
    {
        if (!Enum.IsDefined(condition.Scope))
        {
            issues.Add(AttractionAccessConditionEvidenceIssue.InvalidScope);
        }

        if (condition.Scope != AttractionAccessConditionScope.Attraction
            && string.IsNullOrWhiteSpace(condition.ScopeDetail))
        {
            issues.Add(AttractionAccessConditionEvidenceIssue.MissingScopeDetail);
        }
    }

    private static bool IsValidLanguageCode(string value)
    {
        try
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(value.Trim());
            return !string.IsNullOrWhiteSpace(culture.Name);
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }
}
