namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Composante accompagnée du poids public réellement appliqué.
/// </summary>
public sealed class ParkFitWeightedSubscore
{
    internal ParkFitWeightedSubscore(
        ParkFitSubscore subscore,
        decimal baseWeightPercent,
        decimal applicableWeightPercent,
        decimal? knownScoreWeightPercent,
        decimal? contribution)
    {
        this.Kind = subscore.Kind;
        this.State = subscore.State;
        this.Value = subscore.Value;
        this.CoveragePercent = subscore.CoveragePercent;
        this.Confidence = subscore.Confidence;
        this.Reasons = subscore.Reasons.ToList();
        this.BaseWeightPercent = baseWeightPercent;
        this.ApplicableWeightPercent = applicableWeightPercent;
        this.KnownScoreWeightPercent = knownScoreWeightPercent;
        this.Contribution = contribution;
    }

    public ParkFitSubscoreKind Kind { get; }

    public ParkFitSubscoreState State { get; }

    public decimal? Value { get; }

    public decimal CoveragePercent { get; }

    public ParkFitDataConfidence Confidence { get; }

    public IReadOnlyCollection<ParkFitSubscoreReasonCode> Reasons { get; }

    public decimal BaseWeightPercent { get; }

    public decimal ApplicableWeightPercent { get; }

    public decimal? KnownScoreWeightPercent { get; }

    public decimal? Contribution { get; }
}
