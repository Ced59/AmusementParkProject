namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Résultat comparatif versionné avec ses composantes et plafonds visibles.
/// </summary>
public sealed class ParkFitScore
{
    internal ParkFitScore(
        ParkFitScoreState state,
        decimal? comparativeScore,
        decimal? rawKnownScore,
        decimal knownWeightPercent,
        decimal coveragePercent,
        decimal? scoreCeilingPercent,
        ParkFitDataConfidence confidence,
        ParkFitHardFilterState hardFilterState,
        ParkFitDateAvailabilityState dateAvailabilityState,
        ParkFitUnknownDataPolicy unknownDataPolicy,
        IReadOnlyCollection<ParkFitWeightedSubscore> components,
        IReadOnlyCollection<ParkFitScoreReasonCode> reasons,
        DateOnly evaluationDate,
        DateTime evaluatedAtUtc)
    {
        this.MethodVersion = ParkFitScoreEvaluator.MethodVersion;
        this.State = state;
        this.ComparativeScore = comparativeScore;
        this.RawKnownScore = rawKnownScore;
        this.KnownWeightPercent = knownWeightPercent;
        this.CoveragePercent = coveragePercent;
        this.ScoreCeilingPercent = scoreCeilingPercent;
        this.Confidence = confidence;
        this.HardFilterState = hardFilterState;
        this.DateAvailabilityState = dateAvailabilityState;
        this.UnknownDataPolicy = unknownDataPolicy;
        this.Components = components.ToList();
        this.Reasons = reasons.ToList();
        this.EvaluationDate = evaluationDate;
        this.EvaluatedAtUtc = evaluatedAtUtc;
    }

    public string MethodVersion { get; }

    public ParkFitScoreState State { get; }

    public decimal? ComparativeScore { get; }

    public decimal? RawKnownScore { get; }

    public decimal KnownWeightPercent { get; }

    public decimal CoveragePercent { get; }

    public decimal? ScoreCeilingPercent { get; }

    public ParkFitDataConfidence Confidence { get; }

    public ParkFitHardFilterState HardFilterState { get; }

    public ParkFitDateAvailabilityState DateAvailabilityState { get; }

    public ParkFitUnknownDataPolicy UnknownDataPolicy { get; }

    public IReadOnlyCollection<ParkFitWeightedSubscore> Components { get; }

    public IReadOnlyCollection<ParkFitScoreReasonCode> Reasons { get; }

    public DateOnly EvaluationDate { get; }

    public DateTime EvaluatedAtUtc { get; }
}
