namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Plage civile compatible avec une information historique, sans modifier sa précision.
/// Une borne absente signifie que l'enveloppe est ouverte de ce côté.
/// </summary>
public sealed record HistoricalDateEnvelope
{
    public HistoricalDateEnvelope(
        DateOnly? earliestPossibleDate,
        DateOnly? latestPossibleDate,
        bool hasUncertainPosition)
    {
        if (earliestPossibleDate.HasValue
            && latestPossibleDate.HasValue
            && latestPossibleDate.Value < earliestPossibleDate.Value)
        {
            throw new HistoricalTemporalValidationException(
                HistoricalTemporalErrorCodes.InvalidEnvelope,
                "A historical date envelope cannot end before it starts.",
                nameof(latestPossibleDate));
        }

        this.EarliestPossibleDate = earliestPossibleDate;
        this.LatestPossibleDate = latestPossibleDate;
        this.HasUncertainPosition = hasUncertainPosition;
    }

    public DateOnly? EarliestPossibleDate { get; }

    public DateOnly? LatestPossibleDate { get; }

    public bool HasUncertainPosition { get; }

    public bool IsOpenStart => !this.EarliestPossibleDate.HasValue;

    public bool IsOpenEnd => !this.LatestPossibleDate.HasValue;

    public bool IsExactDay => this.EarliestPossibleDate.HasValue
        && this.LatestPossibleDate.HasValue
        && this.EarliestPossibleDate.Value == this.LatestPossibleDate.Value
        && !this.HasUncertainPosition;

    public bool Contains(DateOnly date)
    {
        return (!this.EarliestPossibleDate.HasValue || date >= this.EarliestPossibleDate.Value)
            && (!this.LatestPossibleDate.HasValue || date <= this.LatestPossibleDate.Value);
    }

    public bool Overlaps(HistoricalDateEnvelope other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return !this.IsDefinitelyBefore(other) && !this.IsDefinitelyAfter(other);
    }

    public bool IsDefinitelyBefore(HistoricalDateEnvelope other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return this.LatestPossibleDate.HasValue
            && other.EarliestPossibleDate.HasValue
            && this.LatestPossibleDate.Value < other.EarliestPossibleDate.Value;
    }

    public bool IsDefinitelyAfter(HistoricalDateEnvelope other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return this.EarliestPossibleDate.HasValue
            && other.LatestPossibleDate.HasValue
            && this.EarliestPossibleDate.Value > other.LatestPossibleDate.Value;
    }
}
