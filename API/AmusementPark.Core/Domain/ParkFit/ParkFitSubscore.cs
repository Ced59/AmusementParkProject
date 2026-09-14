namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Valeur normalisée d'une composante avec sa couverture et sa confiance.
/// </summary>
public sealed class ParkFitSubscore
{
    public ParkFitSubscore(
        ParkFitSubscoreKind kind,
        ParkFitSubscoreState state,
        decimal? value,
        decimal coveragePercent,
        ParkFitDataConfidence confidence,
        IReadOnlyCollection<ParkFitSubscoreReasonCode>? reasons = null)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        if (!Enum.IsDefined(confidence))
        {
            throw new ArgumentOutOfRangeException(nameof(confidence));
        }

        if (coveragePercent is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(coveragePercent));
        }

        if (state == ParkFitSubscoreState.Known)
        {
            if (!value.HasValue || value.Value is < 0m or > 100m)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (coveragePercent == 0m)
            {
                throw new ArgumentException(
                    "A known subscore requires positive factual coverage.",
                    nameof(coveragePercent));
            }
        }
        else if (value.HasValue)
        {
            throw new ArgumentException(
                "Only a known subscore can carry a value.",
                nameof(value));
        }

        if (state == ParkFitSubscoreState.NotApplicable && coveragePercent != 0m)
        {
            throw new ArgumentException(
                "A non-applicable subscore cannot carry factual coverage.",
                nameof(coveragePercent));
        }

        List<ParkFitSubscoreReasonCode> reasonSnapshot = reasons is null
            ? new List<ParkFitSubscoreReasonCode>()
            : reasons.ToList();
        if (reasonSnapshot.Any(static reason => !Enum.IsDefined(reason)))
        {
            throw new ArgumentException(
                "Subscore reasons must contain valid codes.",
                nameof(reasons));
        }

        this.Kind = kind;
        this.State = state;
        this.Value = value;
        this.CoveragePercent = coveragePercent;
        this.Confidence = confidence;
        this.Reasons = reasonSnapshot.Distinct().OrderBy(static reason => reason).ToList();
    }

    public ParkFitSubscoreKind Kind { get; }

    public ParkFitSubscoreState State { get; }

    public decimal? Value { get; }

    public decimal CoveragePercent { get; }

    public ParkFitDataConfidence Confidence { get; }

    public IReadOnlyCollection<ParkFitSubscoreReasonCode> Reasons { get; }
}
