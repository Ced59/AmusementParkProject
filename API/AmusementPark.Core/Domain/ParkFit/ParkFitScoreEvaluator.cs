namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Applique la formule comparative publique après les contraintes éliminatoires.
/// </summary>
public sealed class ParkFitScoreEvaluator
{
    public const string MethodVersion = "park-fit-2026-02";
    public const decimal GroupCompatibilityWeightPercent = 45m;
    public const decimal PreferenceCoverageWeightPercent = 25m;
    public const decimal TravelConvenienceWeightPercent = 15m;
    public const decimal IndoorResilienceWeightPercent = 10m;
    public const decimal BudgetFitWeightPercent = 5m;
    public const decimal MediumConfidenceCeilingPercent = 85m;
    public const decimal LowConfidenceCeilingPercent = 65m;
    public const decimal CriticalUnknownCeilingPercent = 60m;

    public ParkFitScore Evaluate(
        IReadOnlyCollection<ParkFitSubscore> subscores,
        ParkFitHardFilterEvaluation hardFilters,
        ParkFitDateAvailability dateAvailability,
        ParkFitUnknownDataPolicy unknownDataPolicy,
        DateOnly evaluationDate,
        DateTime evaluatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(subscores);
        ArgumentNullException.ThrowIfNull(hardFilters);
        ArgumentNullException.ThrowIfNull(dateAvailability);

        if (!Enum.IsDefined(unknownDataPolicy))
        {
            throw new ArgumentOutOfRangeException(nameof(unknownDataPolicy));
        }

        if (evaluatedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The score evaluation timestamp must use UTC.",
                nameof(evaluatedAtUtc));
        }

        if (hardFilters.EvaluationDate != evaluationDate)
        {
            throw new ArgumentException(
                "Hard filters must match the score evaluation date.",
                nameof(hardFilters));
        }

        if (dateAvailability.EvaluationDate != evaluationDate)
        {
            throw new ArgumentException(
                "Date availability must match the score evaluation date.",
                nameof(dateAvailability));
        }

        List<ParkFitSubscore> snapshot = subscores.ToList();
        ValidateSubscores(snapshot, subscores, evaluationDate);

        decimal applicableWeight = snapshot
            .Where(static subscore => subscore.State != ParkFitSubscoreState.NotApplicable)
            .Sum(static subscore => GetBaseWeight(subscore.Kind));
        decimal knownWeight = snapshot
            .Where(static subscore => subscore.State == ParkFitSubscoreState.Known)
            .Sum(static subscore => GetBaseWeight(subscore.Kind));
        decimal knownWeightPercent = Round(100m * knownWeight / applicableWeight);
        decimal weightedCoveragePercent = Round(snapshot
            .Where(static subscore => subscore.State != ParkFitSubscoreState.NotApplicable)
            .Sum(static subscore => subscore.CoveragePercent * GetBaseWeight(subscore.Kind))
            / applicableWeight);
        decimal coveragePercent = Math.Min(knownWeightPercent, weightedCoveragePercent);
        IReadOnlyCollection<ParkFitWeightedSubscore> components = BuildComponents(
            snapshot,
            applicableWeight,
            knownWeight);
        ParkFitDataConfidence confidence = ResolveConfidence(snapshot);
        bool hasOptionalNotApplicable = snapshot.Any(static subscore =>
            subscore.State == ParkFitSubscoreState.NotApplicable);

        if (hardFilters.State == ParkFitHardFilterState.Failed)
        {
            return BuildWithoutScore(
                ParkFitScoreState.Excluded,
                ParkFitScoreReasonCode.HardFilterFailed,
                knownWeightPercent,
                coveragePercent,
                confidence,
                hardFilters,
                dateAvailability,
                unknownDataPolicy,
                components,
                hasOptionalNotApplicable,
                evaluationDate,
                evaluatedAtUtc);
        }

        if (dateAvailability.State == ParkFitDateAvailabilityState.Unavailable)
        {
            return BuildWithoutScore(
                ParkFitScoreState.Excluded,
                ParkFitScoreReasonCode.DateUnavailable,
                knownWeightPercent,
                coveragePercent,
                confidence,
                hardFilters,
                dateAvailability,
                unknownDataPolicy,
                components,
                hasOptionalNotApplicable,
                evaluationDate,
                evaluatedAtUtc);
        }

        ParkFitSubscore groupCompatibility = snapshot.Single(static subscore =>
            subscore.Kind == ParkFitSubscoreKind.GroupCompatibility);
        int criticalUnknownCount = hardFilters.UnknownFilterCount;

        if (dateAvailability.State == ParkFitDateAvailabilityState.Unknown)
        {
            criticalUnknownCount++;
        }

        if (groupCompatibility.State == ParkFitSubscoreState.Unknown)
        {
            criticalUnknownCount++;
        }

        bool hasCriticalUnknown = criticalUnknownCount > 0;
        if (hasCriticalUnknown
            && unknownDataPolicy == ParkFitUnknownDataPolicy.ExcludeUnknown)
        {
            return BuildWithoutScore(
                ParkFitScoreState.Excluded,
                ParkFitScoreReasonCode.CriticalDataExcluded,
                knownWeightPercent,
                coveragePercent,
                confidence,
                hardFilters,
                dateAvailability,
                unknownDataPolicy,
                components,
                hasOptionalNotApplicable,
                evaluationDate,
                evaluatedAtUtc);
        }

        if (hasCriticalUnknown
            && (unknownDataPolicy == ParkFitUnknownDataPolicy.KnownOnly
                || criticalUnknownCount > 1))
        {
            return BuildWithoutScore(
                ParkFitScoreState.Suspended,
                ParkFitScoreReasonCode.CriticalDataSuspended,
                knownWeightPercent,
                coveragePercent,
                confidence,
                hardFilters,
                dateAvailability,
                unknownDataPolicy,
                components,
                hasOptionalNotApplicable,
                evaluationDate,
                evaluatedAtUtc);
        }

        if (knownWeight == 0m)
        {
            return BuildWithoutScore(
                ParkFitScoreState.Suspended,
                ParkFitScoreReasonCode.NoKnownSubscore,
                knownWeightPercent,
                coveragePercent,
                confidence,
                hardFilters,
                dateAvailability,
                unknownDataPolicy,
                components,
                hasOptionalNotApplicable,
                evaluationDate,
                evaluatedAtUtc);
        }

        if (confidence == ParkFitDataConfidence.Unknown)
        {
            return BuildWithoutScore(
                ParkFitScoreState.Suspended,
                ParkFitScoreReasonCode.DataConfidenceUnknown,
                knownWeightPercent,
                coveragePercent,
                confidence,
                hardFilters,
                dateAvailability,
                unknownDataPolicy,
                components,
                hasOptionalNotApplicable,
                evaluationDate,
                evaluatedAtUtc);
        }

        decimal rawKnownScore = Round(snapshot
            .Where(static subscore => subscore.State == ParkFitSubscoreState.Known)
            .Sum(static subscore => subscore.Value!.Value * GetBaseWeight(subscore.Kind))
            / knownWeight);
        decimal confidenceCeiling = GetConfidenceCeiling(confidence);
        decimal criticalUnknownCeiling = hasCriticalUnknown
            ? CriticalUnknownCeilingPercent
            : 100m;
        decimal scoreCeiling = Math.Min(
            coveragePercent,
            Math.Min(confidenceCeiling, criticalUnknownCeiling));
        decimal comparativeScore = Round(Math.Min(rawKnownScore, scoreCeiling));
        ParkFitScoreState state = comparativeScore < rawKnownScore
            ? ParkFitScoreState.Capped
            : ParkFitScoreState.Available;
        List<ParkFitScoreReasonCode> reasons = BuildScoreReasons(
            state,
            coveragePercent,
            confidenceCeiling,
            hasCriticalUnknown,
            hasOptionalNotApplicable);

        return new ParkFitScore(
            state,
            comparativeScore,
            rawKnownScore,
            knownWeightPercent,
            coveragePercent,
            scoreCeiling < 100m ? Round(scoreCeiling) : null,
            confidence,
            hardFilters,
            dateAvailability,
            unknownDataPolicy,
            components,
            reasons,
            evaluationDate,
            evaluatedAtUtc);
    }

    private static void ValidateSubscores(
        IReadOnlyCollection<ParkFitSubscore> snapshot,
        IReadOnlyCollection<ParkFitSubscore> subscores,
        DateOnly evaluationDate)
    {
        if (snapshot.Any(static subscore => subscore is null))
        {
            throw new ArgumentException(
                "Subscores cannot contain null entries.",
                nameof(subscores));
        }

        IReadOnlyCollection<ParkFitSubscoreKind> expectedKinds =
            Enum.GetValues<ParkFitSubscoreKind>();
        if (snapshot.Count != expectedKinds.Count
            || snapshot.Select(static subscore => subscore.Kind).Distinct().Count()
                != expectedKinds.Count
            || expectedKinds.Any(expectedKind => !snapshot.Any(subscore =>
                subscore.Kind == expectedKind)))
        {
            throw new ArgumentException(
                "Exactly one subscore of every kind is required.",
                nameof(subscores));
        }

        ParkFitSubscore groupCompatibility = snapshot.Single(static subscore =>
            subscore.Kind == ParkFitSubscoreKind.GroupCompatibility);
        ParkFitSubscore preferenceCoverage = snapshot.Single(static subscore =>
            subscore.Kind == ParkFitSubscoreKind.PreferenceCoverage);
        if (groupCompatibility.State == ParkFitSubscoreState.NotApplicable
            || preferenceCoverage.State == ParkFitSubscoreState.NotApplicable)
        {
            throw new ArgumentException(
                "Group compatibility and preference coverage must remain explicit.",
                nameof(subscores));
        }

        if (!groupCompatibility.EvaluationDate.HasValue
            || groupCompatibility.EvaluationDate.Value != evaluationDate)
        {
            throw new ArgumentException(
                "Group compatibility must match the score evaluation date.",
                nameof(subscores));
        }

        ParkFitSubscore budgetFit = snapshot.Single(static subscore =>
            subscore.Kind == ParkFitSubscoreKind.BudgetFit);
        if (budgetFit.State == ParkFitSubscoreState.Known
            && budgetFit.Confidence is not ParkFitDataConfidence.Medium
                and not ParkFitDataConfidence.High)
        {
            throw new ArgumentException(
                "Budget fit requires medium or high confidence.",
                nameof(subscores));
        }
    }

    private static IReadOnlyCollection<ParkFitWeightedSubscore> BuildComponents(
        IEnumerable<ParkFitSubscore> subscores,
        decimal applicableWeight,
        decimal knownWeight)
    {
        List<ParkFitWeightedSubscore> components = new List<ParkFitWeightedSubscore>();
        foreach (ParkFitSubscore subscore in subscores.OrderBy(static subscore => subscore.Kind))
        {
            decimal baseWeight = GetBaseWeight(subscore.Kind);
            decimal applicableWeightPercent = subscore.State == ParkFitSubscoreState.NotApplicable
                ? 0m
                : 100m * baseWeight / applicableWeight;
            decimal? knownScoreWeightPercent = subscore.State == ParkFitSubscoreState.Known
                ? 100m * baseWeight / knownWeight
                : null;
            decimal? contribution = subscore.State == ParkFitSubscoreState.Known
                ? subscore.Value!.Value * baseWeight / knownWeight
                : null;
            components.Add(new ParkFitWeightedSubscore(
                subscore,
                baseWeight,
                applicableWeightPercent,
                knownScoreWeightPercent,
                contribution));
        }

        return components;
    }

    private static ParkFitDataConfidence ResolveConfidence(
        IEnumerable<ParkFitSubscore> subscores)
    {
        List<ParkFitSubscore> knownSubscores = subscores
            .Where(static subscore => subscore.State == ParkFitSubscoreState.Known)
            .ToList();
        return knownSubscores.Count == 0
            ? ParkFitDataConfidence.Unknown
            : knownSubscores.Min(static subscore => subscore.Confidence);
    }

    private static ParkFitScore BuildWithoutScore(
        ParkFitScoreState state,
        ParkFitScoreReasonCode reason,
        decimal knownWeightPercent,
        decimal coveragePercent,
        ParkFitDataConfidence confidence,
        ParkFitHardFilterEvaluation hardFilters,
        ParkFitDateAvailability dateAvailability,
        ParkFitUnknownDataPolicy unknownDataPolicy,
        IReadOnlyCollection<ParkFitWeightedSubscore> components,
        bool hasOptionalNotApplicable,
        DateOnly evaluationDate,
        DateTime evaluatedAtUtc)
    {
        List<ParkFitScoreReasonCode> reasons = new List<ParkFitScoreReasonCode>
        {
            reason,
        };
        if (hasOptionalNotApplicable)
        {
            reasons.Add(ParkFitScoreReasonCode.OptionalSubscoreNotApplicable);
        }

        return new ParkFitScore(
            state,
            null,
            null,
            knownWeightPercent,
            coveragePercent,
            null,
            confidence,
            hardFilters,
            dateAvailability,
            unknownDataPolicy,
            components,
            reasons,
            evaluationDate,
            evaluatedAtUtc);
    }

    private static List<ParkFitScoreReasonCode> BuildScoreReasons(
        ParkFitScoreState state,
        decimal coveragePercent,
        decimal confidenceCeiling,
        bool hasCriticalUnknown,
        bool hasOptionalNotApplicable)
    {
        List<ParkFitScoreReasonCode> reasons = new List<ParkFitScoreReasonCode>();
        if (state == ParkFitScoreState.Available)
        {
            reasons.Add(ParkFitScoreReasonCode.ScoreAvailable);
        }

        if (coveragePercent < 100m)
        {
            reasons.Add(ParkFitScoreReasonCode.IncompleteCoverageCapApplied);
        }

        if (confidenceCeiling < 100m)
        {
            reasons.Add(ParkFitScoreReasonCode.DataConfidenceCapApplied);
        }

        if (hasCriticalUnknown)
        {
            reasons.Add(ParkFitScoreReasonCode.CriticalUnknownCapApplied);
        }

        if (hasOptionalNotApplicable)
        {
            reasons.Add(ParkFitScoreReasonCode.OptionalSubscoreNotApplicable);
        }

        return reasons;
    }

    private static decimal GetConfidenceCeiling(ParkFitDataConfidence confidence)
    {
        return confidence switch
        {
            ParkFitDataConfidence.High => 100m,
            ParkFitDataConfidence.Medium => MediumConfidenceCeilingPercent,
            ParkFitDataConfidence.Low => LowConfidenceCeilingPercent,
            _ => throw new ArgumentOutOfRangeException(nameof(confidence)),
        };
    }

    private static decimal GetBaseWeight(ParkFitSubscoreKind kind)
    {
        return kind switch
        {
            ParkFitSubscoreKind.GroupCompatibility => GroupCompatibilityWeightPercent,
            ParkFitSubscoreKind.PreferenceCoverage => PreferenceCoverageWeightPercent,
            ParkFitSubscoreKind.TravelConvenience => TravelConvenienceWeightPercent,
            ParkFitSubscoreKind.IndoorResilience => IndoorResilienceWeightPercent,
            ParkFitSubscoreKind.BudgetFit => BudgetFitWeightPercent,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    private static decimal Round(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
