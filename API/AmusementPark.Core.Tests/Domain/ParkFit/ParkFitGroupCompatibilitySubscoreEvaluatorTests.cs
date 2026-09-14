using AmusementPark.Core.Domain.ParkFit;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.ParkFit;

public sealed class ParkFitGroupCompatibilitySubscoreEvaluatorTests
{
    private static readonly DateOnly EvaluationDate = new DateOnly(2026, 9, 14);
    private static readonly DateTime EvaluationTimestamp =
        new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);

    private readonly ParkFitGroupCompatibilitySubscoreEvaluator evaluator =
        new ParkFitGroupCompatibilitySubscoreEvaluator();
    private readonly GroupAttractionCompatibilityEvaluator groupEvaluator =
        new GroupAttractionCompatibilityEvaluator();

    [Fact]
    public void Evaluate_WhenThereIsNoAttraction_ShouldReturnUnknown()
    {
        ParkFitSubscore result = this.evaluator.Evaluate(
            Array.Empty<GroupAttractionCompatibility>(),
            EvaluationDate);

        Assert.Equal(ParkFitSubscoreState.Unknown, result.State);
        Assert.Null(result.Value);
        Assert.Equal(0m, result.CoveragePercent);
        Assert.Equal(ParkFitDataConfidence.Unknown, result.Confidence);
        Assert.Equal(EvaluationDate, result.EvaluationDate);
        Assert.Contains(ParkFitSubscoreReasonCode.NoKnownFact, result.Reasons);
    }

    [Theory]
    [InlineData(GroupAttractionCompatibilityState.EveryoneTogether, 100)]
    [InlineData(GroupAttractionCompatibilityState.PossibleWithSplit, 75)]
    [InlineData(GroupAttractionCompatibilityState.None, 0)]
    public void Evaluate_ShouldNormalizeEveryConclusiveGroupOutcome(
        GroupAttractionCompatibilityState state,
        decimal expectedValue)
    {
        GroupAttractionCompatibility compatibility = this.BuildGroup(state);

        ParkFitSubscore result = this.evaluator.Evaluate(
            new[] { compatibility },
            EvaluationDate);

        Assert.Equal(ParkFitSubscoreState.Known, result.State);
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(100m, result.CoveragePercent);
    }

    [Fact]
    public void Evaluate_ShouldUseThePublishedPartialOutcomeValue()
    {
        GroupAttractionCompatibility first = this.groupEvaluator.Evaluate(
            new[]
            {
                BuildMember("a", AttractionCompatibilityState.CompatibleAlone),
                BuildMember("b", AttractionCompatibilityState.Incompatible),
            },
            GroupAttractionParticipationConfiguration.Unknown);
        GroupAttractionCompatibility second = this.groupEvaluator.Evaluate(
            new[]
            {
                BuildMember("a", AttractionCompatibilityState.Incompatible),
                BuildMember("b", AttractionCompatibilityState.CompatibleAlone),
            },
            GroupAttractionParticipationConfiguration.Unknown);

        ParkFitSubscore result = this.evaluator.Evaluate(
            new[] { first, second },
            EvaluationDate);

        Assert.Equal(ParkFitGroupCompatibilitySubscoreEvaluator.PartialValue, result.Value);
    }

    [Fact]
    public void Evaluate_ShouldAverageKnownGroupOutcomes()
    {
        ParkFitSubscore result = this.evaluator.Evaluate(
            new[]
            {
                this.BuildGroup(GroupAttractionCompatibilityState.EveryoneTogether),
                this.BuildGroup(GroupAttractionCompatibilityState.PossibleWithSplit),
            },
            EvaluationDate);

        Assert.Equal(87.5m, result.Value);
        Assert.Equal(100m, result.CoveragePercent);
        Assert.DoesNotContain(
            ParkFitSubscoreReasonCode.MinimumMemberBoundApplied,
            result.Reasons);
    }

    [Fact]
    public void Evaluate_ShouldBoundTheAverageByTheLeastServedMember()
    {
        GroupAttractionCompatibility partial = this.groupEvaluator.Evaluate(
            new[]
            {
                BuildMember("a", AttractionCompatibilityState.CompatibleAlone),
                BuildMember("b", AttractionCompatibilityState.Incompatible),
            },
            GroupAttractionParticipationConfiguration.Unknown);
        GroupAttractionCompatibility together = this.groupEvaluator.Evaluate(
            new[]
            {
                BuildMember("a", AttractionCompatibilityState.CompatibleAlone),
                BuildMember("b", AttractionCompatibilityState.CompatibleAlone),
            },
            GroupAttractionParticipationConfiguration.EveryoneTogether);

        ParkFitSubscore result = this.evaluator.Evaluate(
            new[] { partial, together },
            EvaluationDate);

        Assert.Equal(50m, result.Value);
        Assert.Contains(
            ParkFitSubscoreReasonCode.MinimumMemberBoundApplied,
            result.Reasons);
    }

    [Fact]
    public void Evaluate_ShouldExcludeUnknownGroupOutcomesWithoutTurningThemIntoZero()
    {
        GroupAttractionCompatibility unknown = this.groupEvaluator.Evaluate(
            new[]
            {
                BuildMember("a", AttractionCompatibilityState.CompatibleAlone),
                BuildMember("b", AttractionCompatibilityState.CompatibleAlone),
            },
            GroupAttractionParticipationConfiguration.Unknown);
        GroupAttractionCompatibility together = this.BuildGroup(
            GroupAttractionCompatibilityState.EveryoneTogether);

        ParkFitSubscore result = this.evaluator.Evaluate(
            new[] { unknown, together },
            EvaluationDate);

        Assert.Equal(100m, result.Value);
        Assert.Equal(50m, result.CoveragePercent);
        Assert.Contains(ParkFitSubscoreReasonCode.UnknownFactsExcluded, result.Reasons);
    }

    [Fact]
    public void Evaluate_WhenEveryGroupOutcomeIsUnknown_ShouldReturnUnknown()
    {
        GroupAttractionCompatibility unknown = this.groupEvaluator.Evaluate(
            new[]
            {
                BuildMember("a", AttractionCompatibilityState.CompatibleAlone),
                BuildMember("b", AttractionCompatibilityState.CompatibleAlone),
            },
            GroupAttractionParticipationConfiguration.Unknown);

        ParkFitSubscore result = this.evaluator.Evaluate(
            new[] { unknown },
            EvaluationDate);

        Assert.Equal(ParkFitSubscoreState.Unknown, result.State);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Evaluate_WhenOneMemberHasNoKnownOutcome_ShouldReturnUnknown()
    {
        GroupAttractionCompatibility compatibility = this.groupEvaluator.Evaluate(
            new[]
            {
                BuildMember("known", AttractionCompatibilityState.CompatibleAlone),
                BuildMember("unknown", AttractionCompatibilityState.Unknown),
                BuildMember("incompatible", AttractionCompatibilityState.Incompatible),
            },
            GroupAttractionParticipationConfiguration.Unknown);

        ParkFitSubscore result = this.evaluator.Evaluate(
            new[] { compatibility },
            EvaluationDate);

        Assert.Equal(ParkFitSubscoreState.Unknown, result.State);
        Assert.Null(result.Value);
        Assert.Equal(0m, result.CoveragePercent);
    }

    [Fact]
    public void Evaluate_ShouldUseTheLowestKnownGroupConfidence()
    {
        GroupAttractionCompatibility high = this.BuildGroup(
            GroupAttractionCompatibilityState.EveryoneTogether,
            ParkFitDataConfidence.High);
        GroupAttractionCompatibility low = this.BuildGroup(
            GroupAttractionCompatibilityState.EveryoneTogether,
            ParkFitDataConfidence.Low);

        ParkFitSubscore result = this.evaluator.Evaluate(
            new[] { high, low },
            EvaluationDate);

        Assert.Equal(ParkFitDataConfidence.Low, result.Confidence);
    }

    [Fact]
    public void Evaluate_WhenUnknownGroupOutcomeChangesMemberBound_ShouldUseItsKnownFactConfidence()
    {
        GroupAttractionCompatibility together = this.BuildGroup(
            GroupAttractionCompatibilityState.EveryoneTogether,
            ParkFitDataConfidence.High);
        GroupAttractionCompatibility unknown = this.groupEvaluator.Evaluate(
            new[]
            {
                BuildMember(
                    "a",
                    AttractionCompatibilityState.Incompatible,
                    ParkFitDataConfidence.Low),
                BuildMember(
                    "b",
                    AttractionCompatibilityState.Unknown,
                    ParkFitDataConfidence.Unknown),
            },
            GroupAttractionParticipationConfiguration.Unknown);

        ParkFitSubscore result = this.evaluator.Evaluate(
            new[] { together, unknown },
            EvaluationDate);

        Assert.Equal(50m, result.Value);
        Assert.Equal(ParkFitDataConfidence.Low, result.Confidence);
        Assert.Contains(
            ParkFitSubscoreReasonCode.MinimumMemberBoundApplied,
            result.Reasons);
    }

    [Fact]
    public void Evaluate_WhenPartialOutcomeContainsUnknownMember_ShouldKeepKnownFactConfidence()
    {
        GroupAttractionCompatibility partial = this.groupEvaluator.Evaluate(
            new[]
            {
                BuildMember(
                    "a",
                    AttractionCompatibilityState.CompatibleAlone,
                    ParkFitDataConfidence.High),
                BuildMember(
                    "b",
                    AttractionCompatibilityState.Incompatible,
                    ParkFitDataConfidence.Medium),
                BuildMember(
                    "c",
                    AttractionCompatibilityState.Unknown,
                    ParkFitDataConfidence.Unknown),
            },
            GroupAttractionParticipationConfiguration.Unknown);
        GroupAttractionCompatibility together = this.groupEvaluator.Evaluate(
            new[]
            {
                BuildMember("a", AttractionCompatibilityState.CompatibleAlone),
                BuildMember("b", AttractionCompatibilityState.CompatibleAlone),
                BuildMember("c", AttractionCompatibilityState.CompatibleAlone),
            },
            GroupAttractionParticipationConfiguration.EveryoneTogether);

        ParkFitSubscore result = this.evaluator.Evaluate(
            new[] { partial, together },
            EvaluationDate);

        Assert.Equal(ParkFitSubscoreState.Known, result.State);
        Assert.Equal(50m, result.Value);
        Assert.Equal(50m, result.CoveragePercent);
        Assert.Equal(ParkFitDataConfidence.Medium, result.Confidence);
    }

    [Fact]
    public void Evaluate_ShouldBeInvariantToAttractionOrder()
    {
        GroupAttractionCompatibility together = this.BuildGroup(
            GroupAttractionCompatibilityState.EveryoneTogether);
        GroupAttractionCompatibility split = this.BuildGroup(
            GroupAttractionCompatibilityState.PossibleWithSplit);

        ParkFitSubscore first = this.evaluator.Evaluate(
            new[] { together, split },
            EvaluationDate);
        ParkFitSubscore second = this.evaluator.Evaluate(
            new[] { split, together },
            EvaluationDate);

        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Value, second.Value);
        Assert.Equal(first.CoveragePercent, second.CoveragePercent);
        Assert.Equal(first.Reasons, second.Reasons);
    }

    [Fact]
    public void Evaluate_WhenMemberSetsDiffer_ShouldRejectTheInput()
    {
        GroupAttractionCompatibility first = this.BuildGroup(
            GroupAttractionCompatibilityState.EveryoneTogether);
        GroupAttractionCompatibility second = this.groupEvaluator.Evaluate(
            new[] { BuildMember("a", AttractionCompatibilityState.CompatibleAlone) },
            GroupAttractionParticipationConfiguration.EveryoneTogether);

        Assert.Throws<ArgumentException>(() => this.evaluator.Evaluate(
            new[] { first, second },
            EvaluationDate));
    }

    [Fact]
    public void Evaluate_WhenEvaluationDatesDiffer_ShouldRejectTheInput()
    {
        GroupAttractionCompatibility first = this.BuildGroup(
            GroupAttractionCompatibilityState.EveryoneTogether);
        GroupAttractionCompatibility second = this.groupEvaluator.Evaluate(
            new[]
            {
                BuildMember(
                    "a",
                    AttractionCompatibilityState.CompatibleAlone,
                    evaluationDate: EvaluationDate.AddDays(1)),
                BuildMember(
                    "b",
                    AttractionCompatibilityState.CompatibleAlone,
                    evaluationDate: EvaluationDate.AddDays(1)),
            },
            GroupAttractionParticipationConfiguration.EveryoneTogether);

        Assert.Throws<ArgumentException>(() => this.evaluator.Evaluate(
            new[] { first, second },
            EvaluationDate));
    }

    [Fact]
    public void Evaluate_WhenCollectionIsNull_ShouldRejectTheInput()
    {
        Assert.Throws<ArgumentNullException>(() => this.evaluator.Evaluate(
            null!,
            EvaluationDate));
    }

    [Fact]
    public void Evaluate_WhenCollectionContainsNull_ShouldRejectTheInput()
    {
        Assert.Throws<ArgumentException>(() => this.evaluator.Evaluate(
            new GroupAttractionCompatibility[] { null! },
            EvaluationDate));
    }

    private GroupAttractionCompatibility BuildGroup(
        GroupAttractionCompatibilityState state,
        ParkFitDataConfidence confidence = ParkFitDataConfidence.High)
    {
        return state switch
        {
            GroupAttractionCompatibilityState.EveryoneTogether => this.groupEvaluator.Evaluate(
                new[]
                {
                    BuildMember("a", AttractionCompatibilityState.CompatibleAlone, confidence),
                    BuildMember("b", AttractionCompatibilityState.CompatibleAlone, confidence),
                },
                GroupAttractionParticipationConfiguration.EveryoneTogether),
            GroupAttractionCompatibilityState.PossibleWithSplit => this.groupEvaluator.Evaluate(
                new[]
                {
                    BuildMember("a", AttractionCompatibilityState.CompatibleAlone, confidence),
                    BuildMember("b", AttractionCompatibilityState.CompatibleAlone, confidence),
                },
                GroupAttractionParticipationConfiguration.SplitRequired),
            GroupAttractionCompatibilityState.Partial => this.groupEvaluator.Evaluate(
                new[]
                {
                    BuildMember("a", AttractionCompatibilityState.CompatibleAlone, confidence),
                    BuildMember("b", AttractionCompatibilityState.Incompatible, confidence),
                },
                GroupAttractionParticipationConfiguration.Unknown),
            GroupAttractionCompatibilityState.None => this.groupEvaluator.Evaluate(
                new[]
                {
                    BuildMember("a", AttractionCompatibilityState.Incompatible, confidence),
                    BuildMember("b", AttractionCompatibilityState.Incompatible, confidence),
                },
                GroupAttractionParticipationConfiguration.Unknown),
            _ => throw new ArgumentOutOfRangeException(nameof(state)),
        };
    }

    private static GroupAttractionMemberCompatibility BuildMember(
        string memberKey,
        AttractionCompatibilityState state,
        ParkFitDataConfidence confidence = ParkFitDataConfidence.High,
        DateOnly? evaluationDate = null)
    {
        return new GroupAttractionMemberCompatibility(
            memberKey,
            new AttractionCompatibility
            {
                MethodVersion = AttractionCompatibilityEvaluator.MethodVersion,
                State = state,
                Reasons = Array.Empty<AttractionCompatibilityReason>(),
                Confidence = confidence,
                EvaluatedAtUtc = EvaluationTimestamp,
                EvaluationDate = evaluationDate ?? EvaluationDate,
                Sources = Array.Empty<AttractionCompatibilitySourceReference>(),
            });
    }
}
