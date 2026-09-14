using AmusementPark.Core.Domain.ParkFit;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.ParkFit;

public sealed class ParkFitScoreEvaluatorTests
{
    private static readonly DateOnly EvaluationDate = new DateOnly(2026, 9, 14);
    private static readonly DateTime EvaluationTimestamp =
        new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);

    private readonly ParkFitScoreEvaluator evaluator = new ParkFitScoreEvaluator();

    [Fact]
    public void Evaluate_WhenEverySubscoreIsKnown_ShouldApplyThePublicFormula()
    {
        ParkFitScore result = this.Evaluate(BuildCompleteSubscores());

        Assert.Equal(ParkFitScoreState.Available, result.State);
        Assert.Equal(73m, result.ComparativeScore);
        Assert.Equal(73m, result.RawKnownScore);
        Assert.Equal(100m, result.KnownWeightPercent);
        Assert.Equal(100m, result.CoveragePercent);
        Assert.Null(result.ScoreCeilingPercent);
        Assert.Equal(ParkFitDataConfidence.High, result.Confidence);
        Assert.Equal(ParkFitScoreEvaluator.MethodVersion, result.MethodVersion);
        Assert.Equal(EvaluationDate, result.EvaluationDate);
        Assert.Equal(EvaluationTimestamp, result.EvaluatedAtUtc);
        Assert.Equal(ParkFitScoreReasonCode.ScoreAvailable, Assert.Single(result.Reasons));
    }

    [Fact]
    public void Evaluate_ShouldExposeEveryWeightAndContributionDeterministically()
    {
        ParkFitScore result = this.Evaluate(
            BuildCompleteSubscores().AsEnumerable().Reverse().ToList());

        Assert.Equal(
            Enum.GetValues<ParkFitSubscoreKind>(),
            result.Components.Select(static component => component.Kind));
        Assert.Equal(
            new[] { 45m, 25m, 15m, 10m, 5m },
            result.Components.Select(static component => component.BaseWeightPercent));
        Assert.Equal(
            new decimal?[] { 36m, 15m, 15m, 5m, 2m },
            result.Components.Select(static component => component.Contribution));
    }

    [Fact]
    public void Evaluate_WhenBudgetIsNotApplicable_ShouldRenormalizeVisibleWeights()
    {
        List<ParkFitSubscore> subscores = BuildCompleteSubscores();
        Replace(
            subscores,
            BuildNotApplicable(ParkFitSubscoreKind.BudgetFit));

        ParkFitScore result = this.Evaluate(subscores);

        Assert.Equal(ParkFitScoreState.Available, result.State);
        Assert.Equal(74.74m, result.ComparativeScore);
        Assert.Contains(
            ParkFitScoreReasonCode.OptionalSubscoreNotApplicable,
            result.Reasons);
        ParkFitWeightedSubscore budget = result.Components.Single(static component =>
            component.Kind == ParkFitSubscoreKind.BudgetFit);
        Assert.Equal(0m, budget.ApplicableWeightPercent);
        Assert.Null(budget.KnownScoreWeightPercent);
        Assert.Null(budget.Contribution);
    }

    [Fact]
    public void Evaluate_WhenAHighScoringNonCriticalFactorIsUnknown_ShouldCapByCoverage()
    {
        List<ParkFitSubscore> subscores = BuildKnownSubscores(100m);
        Replace(
            subscores,
            BuildUnknown(ParkFitSubscoreKind.TravelConvenience));

        ParkFitScore result = this.Evaluate(subscores);

        Assert.Equal(ParkFitScoreState.Capped, result.State);
        Assert.Equal(100m, result.RawKnownScore);
        Assert.Equal(85m, result.ComparativeScore);
        Assert.Equal(85m, result.ScoreCeilingPercent);
        Assert.Contains(
            ParkFitScoreReasonCode.IncompleteCoverageCapApplied,
            result.Reasons);
    }

    [Fact]
    public void Evaluate_WhenAFactorIsUnknown_ShouldKeepKnownContributionsExplainable()
    {
        List<ParkFitSubscore> subscores = BuildCompleteSubscores();
        Replace(
            subscores,
            BuildUnknown(ParkFitSubscoreKind.TravelConvenience));

        ParkFitScore result = this.Evaluate(subscores);
        decimal contributionTotal = Math.Round(
            result.Components.Sum(static component => component.Contribution ?? 0m),
            2,
            MidpointRounding.AwayFromZero);

        Assert.Equal(result.RawKnownScore, contributionTotal);
        Assert.Null(result.Components.Single(static component =>
            component.Kind == ParkFitSubscoreKind.TravelConvenience).KnownScoreWeightPercent);
    }

    [Fact]
    public void Evaluate_WhenPartialCoverageIsLowerThanKnownWeight_ShouldUseTheLowerCoverage()
    {
        List<ParkFitSubscore> subscores = BuildKnownSubscores(100m);
        Replace(
            subscores,
            BuildKnown(
                ParkFitSubscoreKind.GroupCompatibility,
                100m,
                coveragePercent: 50m));

        ParkFitScore result = this.Evaluate(subscores);

        Assert.Equal(ParkFitScoreState.Capped, result.State);
        Assert.Equal(77.5m, result.CoveragePercent);
        Assert.Equal(77.5m, result.ComparativeScore);
    }

    [Theory]
    [InlineData(ParkFitDataConfidence.Medium, 85)]
    [InlineData(ParkFitDataConfidence.Low, 65)]
    public void Evaluate_WhenConfidenceIsLimited_ShouldApplyItsCeiling(
        ParkFitDataConfidence confidence,
        decimal expectedScore)
    {
        List<ParkFitSubscore> subscores = BuildKnownSubscores(100m);
        Replace(
            subscores,
            BuildKnown(
                ParkFitSubscoreKind.TravelConvenience,
                100m,
                confidence: confidence));

        ParkFitScore result = this.Evaluate(subscores);

        Assert.Equal(ParkFitScoreState.Capped, result.State);
        Assert.Equal(expectedScore, result.ComparativeScore);
        Assert.Equal(expectedScore, result.ScoreCeilingPercent);
        Assert.Contains(ParkFitScoreReasonCode.DataConfidenceCapApplied, result.Reasons);
    }

    [Fact]
    public void Evaluate_WhenKnownSubscoreConfidenceIsUnknown_ShouldSuspendTheScore()
    {
        List<ParkFitSubscore> subscores = BuildCompleteSubscores();
        Replace(
            subscores,
            BuildKnown(
                ParkFitSubscoreKind.TravelConvenience,
                80m,
                confidence: ParkFitDataConfidence.Unknown));

        ParkFitScore result = this.Evaluate(subscores);

        Assert.Equal(ParkFitScoreState.Suspended, result.State);
        Assert.Null(result.ComparativeScore);
        Assert.Contains(ParkFitScoreReasonCode.DataConfidenceUnknown, result.Reasons);
    }

    [Theory]
    [InlineData(ParkFitUnknownDataPolicy.ExcludeUnknown, ParkFitScoreState.Excluded)]
    [InlineData(ParkFitUnknownDataPolicy.KnownOnly, ParkFitScoreState.Suspended)]
    public void Evaluate_WhenGroupCompatibilityIsUnknown_ShouldApplyStrictPolicy(
        ParkFitUnknownDataPolicy policy,
        ParkFitScoreState expectedState)
    {
        List<ParkFitSubscore> subscores = BuildKnownSubscores(100m);
        Replace(
            subscores,
            BuildUnknown(ParkFitSubscoreKind.GroupCompatibility));

        ParkFitScore result = this.Evaluate(subscores, unknownDataPolicy: policy);

        Assert.Equal(expectedState, result.State);
        Assert.Null(result.ComparativeScore);
        Assert.Contains(
            policy == ParkFitUnknownDataPolicy.ExcludeUnknown
                ? ParkFitScoreReasonCode.CriticalDataExcluded
                : ParkFitScoreReasonCode.CriticalDataSuspended,
            result.Reasons);
    }

    [Fact]
    public void Evaluate_WhenGroupCompatibilityIsUnknownAndWarningsAreAllowed_ShouldCapTheScore()
    {
        List<ParkFitSubscore> subscores = BuildKnownSubscores(100m);
        Replace(
            subscores,
            BuildUnknown(ParkFitSubscoreKind.GroupCompatibility));

        ParkFitScore result = this.Evaluate(
            subscores,
            unknownDataPolicy: ParkFitUnknownDataPolicy.KeepWithWarning);

        Assert.Equal(ParkFitScoreState.Capped, result.State);
        Assert.Equal(55m, result.ComparativeScore);
        Assert.Equal(55m, result.ScoreCeilingPercent);
        Assert.Contains(ParkFitScoreReasonCode.CriticalUnknownCapApplied, result.Reasons);
    }

    [Fact]
    public void Evaluate_WhenMultipleCriticalFactsAreUnknown_ShouldSuspendEvenWithWarnings()
    {
        List<ParkFitSubscore> subscores = BuildKnownSubscores(100m);
        Replace(
            subscores,
            BuildUnknown(ParkFitSubscoreKind.GroupCompatibility));

        ParkFitScore result = this.Evaluate(
            subscores,
            hardFilterState: ParkFitHardFilterState.Unknown,
            unknownDataPolicy: ParkFitUnknownDataPolicy.KeepWithWarning);

        Assert.Equal(ParkFitScoreState.Suspended, result.State);
        Assert.Null(result.ComparativeScore);
        Assert.Contains(ParkFitScoreReasonCode.CriticalDataSuspended, result.Reasons);
    }

    [Theory]
    [InlineData(ParkFitHardFilterState.Unknown, ParkFitDateAvailabilityState.Available)]
    [InlineData(ParkFitHardFilterState.Passed, ParkFitDateAvailabilityState.Unknown)]
    public void Evaluate_WhenAnotherCriticalFactIsUnknown_ShouldApplyTheSelectedPolicy(
        ParkFitHardFilterState hardFilterState,
        ParkFitDateAvailabilityState dateAvailabilityState)
    {
        ParkFitScore result = this.Evaluate(
            BuildKnownSubscores(100m),
            hardFilterState,
            dateAvailabilityState,
            ParkFitUnknownDataPolicy.KnownOnly);

        Assert.Equal(ParkFitScoreState.Suspended, result.State);
        Assert.Null(result.ComparativeScore);
    }

    [Fact]
    public void Evaluate_WhenHardFilterFails_ShouldExcludeBeforeScoring()
    {
        ParkFitScore result = this.Evaluate(
            BuildCompleteSubscores(),
            hardFilterState: ParkFitHardFilterState.Failed);

        Assert.Equal(ParkFitScoreState.Excluded, result.State);
        Assert.Null(result.ComparativeScore);
        Assert.Contains(ParkFitScoreReasonCode.HardFilterFailed, result.Reasons);
    }

    [Fact]
    public void Evaluate_WhenDateIsUnavailable_ShouldExcludeBeforeScoring()
    {
        ParkFitScore result = this.Evaluate(
            BuildCompleteSubscores(),
            dateAvailabilityState: ParkFitDateAvailabilityState.Unavailable);

        Assert.Equal(ParkFitScoreState.Excluded, result.State);
        Assert.Null(result.ComparativeScore);
        Assert.Contains(ParkFitScoreReasonCode.DateUnavailable, result.Reasons);
    }

    [Fact]
    public void Evaluate_WhenOnlyNonCriticalDataIsUnknown_ShouldNotSuspendKnownOnlyPolicy()
    {
        List<ParkFitSubscore> subscores = BuildKnownSubscores(100m);
        Replace(
            subscores,
            BuildUnknown(ParkFitSubscoreKind.IndoorResilience));

        ParkFitScore result = this.Evaluate(
            subscores,
            unknownDataPolicy: ParkFitUnknownDataPolicy.KnownOnly);

        Assert.Equal(ParkFitScoreState.Capped, result.State);
        Assert.Equal(90m, result.ComparativeScore);
    }

    [Fact]
    public void Evaluate_WhenNoSubscoreIsKnown_ShouldSuspendTheScore()
    {
        List<ParkFitSubscore> subscores = Enum.GetValues<ParkFitSubscoreKind>()
            .Select(BuildUnknown)
            .ToList();

        ParkFitScore result = this.Evaluate(
            subscores,
            unknownDataPolicy: ParkFitUnknownDataPolicy.KeepWithWarning);

        Assert.Equal(ParkFitScoreState.Suspended, result.State);
        Assert.Contains(ParkFitScoreReasonCode.NoKnownSubscore, result.Reasons);
    }

    [Fact]
    public void Evaluate_WhenBudgetConfidenceIsInsufficient_ShouldRejectTheInput()
    {
        List<ParkFitSubscore> subscores = BuildCompleteSubscores();
        Replace(
            subscores,
            BuildKnown(
                ParkFitSubscoreKind.BudgetFit,
                50m,
                confidence: ParkFitDataConfidence.Low));

        Assert.Throws<ArgumentException>(() => this.Evaluate(subscores));
    }

    [Fact]
    public void Evaluate_WhenARequiredSubscoreIsNotApplicable_ShouldRejectTheInput()
    {
        List<ParkFitSubscore> subscores = BuildCompleteSubscores();
        Replace(
            subscores,
            BuildNotApplicable(ParkFitSubscoreKind.GroupCompatibility));

        Assert.Throws<ArgumentException>(() => this.Evaluate(subscores));
    }

    [Fact]
    public void Evaluate_WhenASubscoreKindIsMissing_ShouldRejectTheInput()
    {
        List<ParkFitSubscore> subscores = BuildCompleteSubscores();
        subscores.RemoveAt(0);

        Assert.Throws<ArgumentException>(() => this.Evaluate(subscores));
    }

    [Fact]
    public void Evaluate_WhenASubscoreKindIsDuplicated_ShouldRejectTheInput()
    {
        List<ParkFitSubscore> subscores = BuildCompleteSubscores();
        subscores[^1] = BuildKnown(ParkFitSubscoreKind.GroupCompatibility, 50m);

        Assert.Throws<ArgumentException>(() => this.Evaluate(subscores));
    }

    [Fact]
    public void Evaluate_WhenGroupSubscoreUsesAnotherDate_ShouldRejectTheInput()
    {
        List<ParkFitSubscore> subscores = BuildCompleteSubscores();
        Replace(
            subscores,
            BuildKnown(
                ParkFitSubscoreKind.GroupCompatibility,
                50m,
                evaluationDate: EvaluationDate.AddDays(-1)));

        Assert.Throws<ArgumentException>(() => this.Evaluate(subscores));
    }

    [Fact]
    public void Evaluate_WhenSubscoreCollectionIsNull_ShouldRejectTheInput()
    {
        Assert.Throws<ArgumentNullException>(() => this.evaluator.Evaluate(
            null!,
            ParkFitHardFilterState.Passed,
            ParkFitDateAvailabilityState.Available,
            ParkFitUnknownDataPolicy.KeepWithWarning,
            EvaluationDate,
            EvaluationTimestamp));
    }

    [Fact]
    public void Evaluate_WhenSubscoreCollectionContainsNull_ShouldRejectTheInput()
    {
        List<ParkFitSubscore> subscores = BuildCompleteSubscores();
        subscores[0] = null!;

        Assert.Throws<ArgumentException>(() => this.Evaluate(subscores));
    }

    [Theory]
    [InlineData(99, 0, 0)]
    [InlineData(0, 99, 0)]
    [InlineData(0, 0, 99)]
    public void Evaluate_WhenAnInputEnumIsInvalid_ShouldRejectTheInput(
        int hardFilterState,
        int dateAvailabilityState,
        int unknownDataPolicy)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => this.Evaluate(
            BuildCompleteSubscores(),
            (ParkFitHardFilterState)hardFilterState,
            (ParkFitDateAvailabilityState)dateAvailabilityState,
            (ParkFitUnknownDataPolicy)unknownDataPolicy));
    }

    [Fact]
    public void Evaluate_WhenTimestampIsNotUtc_ShouldRejectTheInput()
    {
        Assert.Throws<ArgumentException>(() => this.evaluator.Evaluate(
            BuildCompleteSubscores(),
            ParkFitHardFilterState.Passed,
            ParkFitDateAvailabilityState.Available,
            ParkFitUnknownDataPolicy.KeepWithWarning,
            EvaluationDate,
            DateTime.SpecifyKind(EvaluationTimestamp, DateTimeKind.Local)));
    }

    private ParkFitScore Evaluate(
        IReadOnlyCollection<ParkFitSubscore> subscores,
        ParkFitHardFilterState hardFilterState = ParkFitHardFilterState.Passed,
        ParkFitDateAvailabilityState dateAvailabilityState = ParkFitDateAvailabilityState.Available,
        ParkFitUnknownDataPolicy unknownDataPolicy = ParkFitUnknownDataPolicy.KeepWithWarning)
    {
        return this.evaluator.Evaluate(
            subscores,
            hardFilterState,
            dateAvailabilityState,
            unknownDataPolicy,
            EvaluationDate,
            EvaluationTimestamp);
    }

    private static List<ParkFitSubscore> BuildCompleteSubscores()
    {
        return new List<ParkFitSubscore>
        {
            BuildKnown(ParkFitSubscoreKind.GroupCompatibility, 80m),
            BuildKnown(ParkFitSubscoreKind.PreferenceCoverage, 60m),
            BuildKnown(ParkFitSubscoreKind.TravelConvenience, 100m),
            BuildKnown(ParkFitSubscoreKind.IndoorResilience, 50m),
            BuildKnown(ParkFitSubscoreKind.BudgetFit, 40m),
        };
    }

    private static List<ParkFitSubscore> BuildKnownSubscores(decimal value)
    {
        return Enum.GetValues<ParkFitSubscoreKind>()
            .Select(kind => BuildKnown(kind, value))
            .ToList();
    }

    private static ParkFitSubscore BuildKnown(
        ParkFitSubscoreKind kind,
        decimal value,
        decimal coveragePercent = 100m,
        ParkFitDataConfidence confidence = ParkFitDataConfidence.High,
        DateOnly? evaluationDate = null)
    {
        return new ParkFitSubscore(
            kind,
            ParkFitSubscoreState.Known,
            value,
            coveragePercent,
            confidence,
            new[] { ParkFitSubscoreReasonCode.KnownFactsNormalized },
            evaluationDate ?? (kind == ParkFitSubscoreKind.GroupCompatibility
                ? EvaluationDate
                : null));
    }

    private static ParkFitSubscore BuildUnknown(ParkFitSubscoreKind kind)
    {
        return new ParkFitSubscore(
            kind,
            ParkFitSubscoreState.Unknown,
            null,
            0m,
            ParkFitDataConfidence.Unknown,
            new[] { ParkFitSubscoreReasonCode.NoKnownFact },
            kind == ParkFitSubscoreKind.GroupCompatibility ? EvaluationDate : null);
    }

    private static ParkFitSubscore BuildNotApplicable(ParkFitSubscoreKind kind)
    {
        return new ParkFitSubscore(
            kind,
            ParkFitSubscoreState.NotApplicable,
            null,
            0m,
            ParkFitDataConfidence.Unknown);
    }

    private static void Replace(
        IList<ParkFitSubscore> subscores,
        ParkFitSubscore replacement)
    {
        int index = subscores
            .Select((subscore, position) => new { subscore, position })
            .Single(item => item.subscore.Kind == replacement.Kind)
            .position;
        subscores[index] = replacement;
    }
}
