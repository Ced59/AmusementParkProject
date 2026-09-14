using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class AttractionAccessConditionSemanticEvaluatorTests
{
    [Theory]
    [InlineData(AttractionAccessConditionType.MinHeight, 120d, AttractionAccessConditionUnit.Centimeter)]
    [InlineData(AttractionAccessConditionType.MinHeight, 300d, AttractionAccessConditionUnit.Centimeter)]
    [InlineData(AttractionAccessConditionType.MaxHeight, 48d, AttractionAccessConditionUnit.Inch)]
    [InlineData(AttractionAccessConditionType.MinAge, 8d, AttractionAccessConditionUnit.Year)]
    public void Evaluate_WhenNumericRuleIsUsable_ShouldReturnNoIssue(
        AttractionAccessConditionType type,
        double value,
        AttractionAccessConditionUnit unit)
    {
        AttractionAccessCondition condition = new AttractionAccessCondition
        {
            Type = type,
            Value = value,
            Unit = unit,
        };

        IReadOnlyCollection<AttractionAccessConditionSemanticIssue> issues =
            AttractionAccessConditionSemanticEvaluator.Evaluate(condition);

        Assert.Empty(issues);
    }

    [Fact]
    public void Evaluate_WhenAgeRuleHasNoValueAndWrongUnit_ShouldReturnEveryIssue()
    {
        AttractionAccessCondition condition = new AttractionAccessCondition
        {
            Type = AttractionAccessConditionType.MinAge,
            Unit = AttractionAccessConditionUnit.Centimeter,
        };

        IReadOnlyCollection<AttractionAccessConditionSemanticIssue> issues =
            AttractionAccessConditionSemanticEvaluator.Evaluate(condition);

        Assert.Contains(AttractionAccessConditionSemanticIssue.MissingValue, issues);
        Assert.Contains(AttractionAccessConditionSemanticIssue.InvalidUnit, issues);
    }

    [Fact]
    public void Evaluate_WhenCustomRuleHasNoStableDefinition_ShouldReturnIssue()
    {
        AttractionAccessCondition condition = new AttractionAccessCondition
        {
            Type = AttractionAccessConditionType.Custom,
        };

        IReadOnlyCollection<AttractionAccessConditionSemanticIssue> issues =
            AttractionAccessConditionSemanticEvaluator.Evaluate(condition);

        Assert.Equal(
            AttractionAccessConditionSemanticIssue.MissingCustomDefinition,
            Assert.Single(issues));
    }

    [Fact]
    public void Evaluate_WhenCustomRuleHasLocalizedDefinition_ShouldReturnNoIssue()
    {
        AttractionAccessCondition condition = new AttractionAccessCondition
        {
            Type = AttractionAccessConditionType.Custom,
            CustomTypeLabel = new List<LocalizedText>
            {
                new LocalizedText("fr", "Accès spécifique"),
            },
        };

        Assert.Empty(AttractionAccessConditionSemanticEvaluator.Evaluate(condition));
    }

    [Fact]
    public void Evaluate_WhenAccompaniedRuleExplicitlyRejectsAccompaniment_ShouldReturnIssue()
    {
        AttractionAccessCondition condition = new AttractionAccessCondition
        {
            Type = AttractionAccessConditionType.MinHeightAccompanied,
            Value = 100,
            Unit = AttractionAccessConditionUnit.Centimeter,
            RequiresAccompaniment = false,
        };

        IReadOnlyCollection<AttractionAccessConditionSemanticIssue> issues =
            AttractionAccessConditionSemanticEvaluator.Evaluate(condition);

        Assert.Equal(
            AttractionAccessConditionSemanticIssue.InconsistentAccompaniment,
            Assert.Single(issues));
    }

    [Fact]
    public void Evaluate_WhenCompanionAgeHasNoAccompanimentSignal_ShouldReturnIssue()
    {
        AttractionAccessCondition condition = new AttractionAccessCondition
        {
            Type = AttractionAccessConditionType.MinHeight,
            Value = 100,
            Unit = AttractionAccessConditionUnit.Centimeter,
            MinimumCompanionAge = 18,
        };

        IReadOnlyCollection<AttractionAccessConditionSemanticIssue> issues =
            AttractionAccessConditionSemanticEvaluator.Evaluate(condition);

        Assert.Equal(
            AttractionAccessConditionSemanticIssue.InconsistentAccompaniment,
            Assert.Single(issues));
    }

    [Fact]
    public void Evaluate_WhenCompanionAgeExceedsTheSupportedDomain_ShouldReturnIssue()
    {
        AttractionAccessCondition condition = new AttractionAccessCondition
        {
            Type = AttractionAccessConditionType.MinHeightAccompanied,
            Value = 100,
            Unit = AttractionAccessConditionUnit.Centimeter,
            RequiresAccompaniment = true,
            MinimumCompanionAge = 131,
        };

        IReadOnlyCollection<AttractionAccessConditionSemanticIssue> issues =
            AttractionAccessConditionSemanticEvaluator.Evaluate(condition);

        Assert.Equal(
            AttractionAccessConditionSemanticIssue.InvalidCompanionAge,
            Assert.Single(issues));
    }

    [Fact]
    public void Evaluate_WhenVisitorAgeThresholdExceedsTheSupportedDomain_ShouldReturnIssue()
    {
        AttractionAccessCondition condition = new AttractionAccessCondition
        {
            Type = AttractionAccessConditionType.MinAge,
            Value = 131,
            Unit = AttractionAccessConditionUnit.Year,
        };

        IReadOnlyCollection<AttractionAccessConditionSemanticIssue> issues =
            AttractionAccessConditionSemanticEvaluator.Evaluate(condition);

        Assert.Equal(
            AttractionAccessConditionSemanticIssue.InvalidValue,
            Assert.Single(issues));
    }

    [Theory]
    [InlineData(301d, AttractionAccessConditionUnit.Centimeter)]
    [InlineData(119d, AttractionAccessConditionUnit.Inch)]
    public void Evaluate_WhenHeightThresholdExceedsTheSupportedDomain_ShouldReturnIssue(
        double value,
        AttractionAccessConditionUnit unit)
    {
        AttractionAccessCondition condition = new AttractionAccessCondition
        {
            Type = AttractionAccessConditionType.MinHeight,
            Value = value,
            Unit = unit,
        };

        IReadOnlyCollection<AttractionAccessConditionSemanticIssue> issues =
            AttractionAccessConditionSemanticEvaluator.Evaluate(condition);

        Assert.Equal(
            AttractionAccessConditionSemanticIssue.InvalidValue,
            Assert.Single(issues));
    }
}
