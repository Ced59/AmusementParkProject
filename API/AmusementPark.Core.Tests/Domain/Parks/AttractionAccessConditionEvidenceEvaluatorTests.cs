using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class AttractionAccessConditionEvidenceEvaluatorTests
{
    private static readonly DateTime EvaluationTimestamp =
        new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(AttractionAccessConditionSourceKind.Official)]
    [InlineData(AttractionAccessConditionSourceKind.OperatorProvided)]
    public void Evaluate_WhenEvidenceIsAuthoritativeAndCurrent_ShouldBeDecisionEligible(
        AttractionAccessConditionSourceKind sourceKind)
    {
        AttractionAccessCondition condition = BuildDecisionEligibleCondition();
        condition.SourceKind = sourceKind;

        IReadOnlyCollection<AttractionAccessConditionEvidenceIssue> issues =
            AttractionAccessConditionEvidenceEvaluator.Evaluate(
                condition,
                EvaluationTimestamp,
                TimeSpan.FromDays(365));

        Assert.Empty(issues);
        Assert.True(AttractionAccessConditionEvidenceEvaluator.IsDecisionEligible(
            condition,
            EvaluationTimestamp,
            TimeSpan.FromDays(365)));
    }

    [Fact]
    public void Evaluate_WhenConditionIsMigratedLegacyData_ShouldExposeMissingEvidenceWithoutInventingIt()
    {
        AttractionAccessCondition condition = new AttractionAccessCondition
        {
            Type = AttractionAccessConditionType.MinHeight,
            Value = 120,
            Unit = AttractionAccessConditionUnit.Centimeter,
        };

        IReadOnlyCollection<AttractionAccessConditionEvidenceIssue> issues =
            AttractionAccessConditionEvidenceEvaluator.Evaluate(
                condition,
                EvaluationTimestamp,
                TimeSpan.FromDays(365));

        Assert.Contains(AttractionAccessConditionEvidenceIssue.SourceNotDecisionEligible, issues);
        Assert.Contains(AttractionAccessConditionEvidenceIssue.MissingSourceReference, issues);
        Assert.Contains(AttractionAccessConditionEvidenceIssue.MissingCollectionTimestamp, issues);
        Assert.Contains(AttractionAccessConditionEvidenceIssue.MissingVerificationTimestamp, issues);
        Assert.Contains(AttractionAccessConditionEvidenceIssue.MissingSourceLanguage, issues);
        Assert.Contains(AttractionAccessConditionEvidenceIssue.MissingSourceSummary, issues);
        Assert.Contains(AttractionAccessConditionEvidenceIssue.InsufficientConfidence, issues);
        Assert.DoesNotContain(AttractionAccessConditionEvidenceIssue.UnsupportedSchemaVersion, issues);
        Assert.DoesNotContain(AttractionAccessConditionEvidenceIssue.MissingScopeDetail, issues);
    }

    [Theory]
    [InlineData(AttractionAccessConditionSourceKind.VerifiedSecondary)]
    [InlineData(AttractionAccessConditionSourceKind.CommunityUnverified)]
    public void Evaluate_WhenSourceIsNotAuthoritative_ShouldRejectDecisionUse(
        AttractionAccessConditionSourceKind sourceKind)
    {
        AttractionAccessCondition condition = BuildDecisionEligibleCondition();
        condition.SourceKind = sourceKind;

        IReadOnlyCollection<AttractionAccessConditionEvidenceIssue> issues =
            AttractionAccessConditionEvidenceEvaluator.Evaluate(
                condition,
                EvaluationTimestamp,
                TimeSpan.FromDays(365));

        Assert.Contains(AttractionAccessConditionEvidenceIssue.SourceNotDecisionEligible, issues);
    }

    [Fact]
    public void Evaluate_WhenEvidenceHasInvalidValues_ShouldReportEveryIndependentIssue()
    {
        AttractionAccessCondition condition = BuildDecisionEligibleCondition();
        condition.ProvenanceSchemaVersion = 99;
        condition.SourceUrl = "ftp://example.test/restrictions";
        condition.SourceLanguageCode = "!";
        condition.SourceSummary.Clear();
        condition.SourceConfidence = AttractionAccessConditionConfidence.Low;
        condition.Scope = AttractionAccessConditionScope.Seat;
        condition.ScopeDetail = " ";
        condition.EffectiveFrom = new DateOnly(2026, 10, 1);
        condition.EffectiveTo = new DateOnly(2026, 9, 1);

        IReadOnlyCollection<AttractionAccessConditionEvidenceIssue> issues =
            AttractionAccessConditionEvidenceEvaluator.Evaluate(
                condition,
                EvaluationTimestamp,
                TimeSpan.FromDays(365));

        Assert.Contains(AttractionAccessConditionEvidenceIssue.UnsupportedSchemaVersion, issues);
        Assert.Contains(AttractionAccessConditionEvidenceIssue.InvalidSourceUrl, issues);
        Assert.Contains(AttractionAccessConditionEvidenceIssue.InvalidSourceLanguage, issues);
        Assert.Contains(AttractionAccessConditionEvidenceIssue.MissingSourceSummary, issues);
        Assert.Contains(AttractionAccessConditionEvidenceIssue.InsufficientConfidence, issues);
        Assert.Contains(AttractionAccessConditionEvidenceIssue.MissingScopeDetail, issues);
        Assert.Contains(AttractionAccessConditionEvidenceIssue.InvalidEffectivePeriod, issues);
    }

    [Fact]
    public void Evaluate_WhenTimestampsConflict_ShouldReportChronologyAndUtcIssues()
    {
        AttractionAccessCondition condition = BuildDecisionEligibleCondition();
        condition.CollectedAtUtc = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Unspecified);
        condition.VerifiedAtUtc = new DateTime(2026, 9, 15, 9, 0, 0, DateTimeKind.Unspecified);

        IReadOnlyCollection<AttractionAccessConditionEvidenceIssue> issues =
            AttractionAccessConditionEvidenceEvaluator.Evaluate(
                condition,
                EvaluationTimestamp,
                TimeSpan.FromDays(365));

        Assert.Contains(AttractionAccessConditionEvidenceIssue.TimestampNotUtc, issues);
        Assert.Contains(AttractionAccessConditionEvidenceIssue.VerificationBeforeCollection, issues);
        Assert.Contains(AttractionAccessConditionEvidenceIssue.VerificationInFuture, issues);
    }

    [Fact]
    public void Evaluate_WhenVerificationIsTooOld_ShouldReportStaleness()
    {
        AttractionAccessCondition condition = BuildDecisionEligibleCondition();
        condition.CollectedAtUtc = EvaluationTimestamp.AddDays(-500);
        condition.VerifiedAtUtc = EvaluationTimestamp.AddDays(-400);

        IReadOnlyCollection<AttractionAccessConditionEvidenceIssue> issues =
            AttractionAccessConditionEvidenceEvaluator.Evaluate(
                condition,
                EvaluationTimestamp,
                TimeSpan.FromDays(365));

        Assert.Contains(AttractionAccessConditionEvidenceIssue.VerificationStale, issues);
    }

    [Fact]
    public void Evaluate_WhenVerificationIsMissingAndCollectionTimestampIsNotUtc_ShouldReportBothIssues()
    {
        AttractionAccessCondition condition = BuildDecisionEligibleCondition();
        condition.CollectedAtUtc = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Unspecified);
        condition.VerifiedAtUtc = null;

        IReadOnlyCollection<AttractionAccessConditionEvidenceIssue> issues =
            AttractionAccessConditionEvidenceEvaluator.Evaluate(
                condition,
                EvaluationTimestamp,
                TimeSpan.FromDays(365));

        Assert.Contains(AttractionAccessConditionEvidenceIssue.MissingVerificationTimestamp, issues);
        Assert.Contains(AttractionAccessConditionEvidenceIssue.TimestampNotUtc, issues);
    }

    [Fact]
    public void Evaluate_WhenEnumValuesAreUnknown_ShouldNotMakeEvidenceEligible()
    {
        AttractionAccessCondition condition = BuildDecisionEligibleCondition();
        condition.SourceConfidence = (AttractionAccessConditionConfidence)999;
        condition.Scope = (AttractionAccessConditionScope)999;
        condition.ScopeDetail = "Unsupported scope";

        IReadOnlyCollection<AttractionAccessConditionEvidenceIssue> issues =
            AttractionAccessConditionEvidenceEvaluator.Evaluate(
                condition,
                EvaluationTimestamp,
                TimeSpan.FromDays(365));

        Assert.Contains(AttractionAccessConditionEvidenceIssue.InsufficientConfidence, issues);
        Assert.Contains(AttractionAccessConditionEvidenceIssue.InvalidScope, issues);
        Assert.False(AttractionAccessConditionEvidenceEvaluator.IsDecisionEligible(
            condition,
            EvaluationTimestamp,
            TimeSpan.FromDays(365)));
    }

    [Fact]
    public void Evaluate_WhenEvaluationContractIsInvalid_ShouldFailFast()
    {
        AttractionAccessCondition condition = BuildDecisionEligibleCondition();

        Assert.Throws<ArgumentException>(() => AttractionAccessConditionEvidenceEvaluator.Evaluate(
            condition,
            DateTime.SpecifyKind(EvaluationTimestamp, DateTimeKind.Local),
            TimeSpan.FromDays(365)));
        Assert.Throws<ArgumentOutOfRangeException>(() => AttractionAccessConditionEvidenceEvaluator.Evaluate(
            condition,
            EvaluationTimestamp,
            TimeSpan.FromDays(-1)));
    }

    private static AttractionAccessCondition BuildDecisionEligibleCondition()
    {
        return new AttractionAccessCondition
        {
            Type = AttractionAccessConditionType.MinHeight,
            Value = 120,
            Unit = AttractionAccessConditionUnit.Centimeter,
            ProvenanceSchemaVersion = AttractionAccessCondition.CurrentProvenanceSchemaVersion,
            SourceKind = AttractionAccessConditionSourceKind.Official,
            SourceUrl = "https://example.test/restrictions",
            CollectedAtUtc = EvaluationTimestamp.AddDays(-30),
            VerifiedAtUtc = EvaluationTimestamp.AddDays(-15),
            SourceLanguageCode = "fr",
            SourceSummary = new List<LocalizedText>
            {
                new LocalizedText("fr", "Taille minimale de 120 cm."),
            },
            SourceConfidence = AttractionAccessConditionConfidence.High,
            Scope = AttractionAccessConditionScope.Attraction,
            EffectiveFrom = new DateOnly(2026, 1, 1),
        };
    }
}
