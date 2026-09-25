namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Période historique inclusive pouvant posséder une borne ouverte.
/// </summary>
public sealed record HistoricalPeriod
{
    public HistoricalPeriod(
        HistoricalDate? start,
        HistoricalDate? end,
        PeriodBoundaryConfidence startConfidence,
        PeriodBoundaryConfidence endConfidence)
    {
        if (start is null && end is null)
        {
            throw Invalid(
                HistoricalTemporalErrorCodes.PeriodRequiresBoundary,
                "A historical period requires at least one boundary.",
                nameof(start));
        }

        ValidateConfidence(startConfidence, nameof(startConfidence));
        ValidateConfidence(endConfidence, nameof(endConfidence));
        ValidateOpenBoundaryConfidence(start, startConfidence, nameof(startConfidence));
        ValidateOpenBoundaryConfidence(end, endConfidence, nameof(endConfidence));

        HistoricalPeriodOrdering ordering = ResolveOrdering(start, end);

        this.Start = start;
        this.End = end;
        this.StartConfidence = startConfidence;
        this.EndConfidence = endConfidence;
        this.Ordering = ordering;
    }

    public HistoricalDate? Start { get; }

    public HistoricalDate? End { get; }

    public PeriodBoundaryConfidence StartConfidence { get; }

    public PeriodBoundaryConfidence EndConfidence { get; }

    public HistoricalPeriodOrdering Ordering { get; }

    public bool IsPoint => this.Ordering == HistoricalPeriodOrdering.Point;

    public bool HasOpenStart => this.Start is null;

    public bool HasOpenEnd => this.End is null;

    public bool HasUncertainBoundary => (this.Start is not null
            && (this.StartConfidence != PeriodBoundaryConfidence.Confirmed
                || this.Start.HasUncertainBoundary))
        || (this.End is not null
            && (this.EndConfidence != PeriodBoundaryConfidence.Confirmed
                || this.End.HasUncertainBoundary));

    public static HistoricalPeriod Point(
        HistoricalDate date,
        PeriodBoundaryConfidence confidence = PeriodBoundaryConfidence.Confirmed)
    {
        ArgumentNullException.ThrowIfNull(date);

        return new HistoricalPeriod(date, date, confidence, confidence);
    }

    public static HistoricalPeriod From(
        HistoricalDate start,
        PeriodBoundaryConfidence confidence = PeriodBoundaryConfidence.Confirmed)
    {
        ArgumentNullException.ThrowIfNull(start);

        return new HistoricalPeriod(
            start,
            null,
            confidence,
            PeriodBoundaryConfidence.Confirmed);
    }

    public static HistoricalPeriod Until(
        HistoricalDate end,
        PeriodBoundaryConfidence confidence = PeriodBoundaryConfidence.Confirmed)
    {
        ArgumentNullException.ThrowIfNull(end);

        return new HistoricalPeriod(
            null,
            end,
            PeriodBoundaryConfidence.Confirmed,
            confidence);
    }

    public HistoricalDateEnvelope GetPossibleEnvelope()
    {
        DateOnly? earliestPossibleDate = this.Start?.GetEnvelope().EarliestPossibleDate;
        DateOnly? latestPossibleDate = this.End?.GetEnvelope().LatestPossibleDate;

        return new HistoricalDateEnvelope(
            earliestPossibleDate,
            latestPossibleDate,
            this.HasUncertainBoundary || this.Ordering == HistoricalPeriodOrdering.Ambiguous);
    }

    public HistoricalPeriodMatch Match(HistoricalInstant instant)
    {
        ArgumentNullException.ThrowIfNull(instant);

        HistoricalDateEnvelope instantEnvelope = instant.GetEnvelope();
        if (!this.GetPossibleEnvelope().Overlaps(instantEnvelope))
        {
            return HistoricalPeriodMatch.Outside;
        }

        if (this.ContainsEntireEnvelope(instantEnvelope))
        {
            return HistoricalPeriodMatch.EntirelyContained;
        }

        if (this.HasDefiniteOverlap(instantEnvelope))
        {
            return HistoricalPeriodMatch.DefinitePartialOverlap;
        }

        return HistoricalPeriodMatch.PossibleOverlap;
    }

    private bool ContainsEntireEnvelope(HistoricalDateEnvelope instantEnvelope)
    {
        if (this.Ordering == HistoricalPeriodOrdering.Ambiguous)
        {
            return false;
        }

        if (this.Start is not null)
        {
            if (this.StartConfidence != PeriodBoundaryConfidence.Confirmed)
            {
                return false;
            }

            if (this.Start.IsApproximate)
            {
                return false;
            }

            DateOnly? latestPossibleStart = this.Start.GetEnvelope().LatestPossibleDate;
            if (!latestPossibleStart.HasValue
                || !instantEnvelope.EarliestPossibleDate.HasValue
                || instantEnvelope.EarliestPossibleDate.Value < latestPossibleStart.Value
                || (this.Start.HasUncertainBoundary
                    && instantEnvelope.EarliestPossibleDate.Value == latestPossibleStart.Value))
            {
                return false;
            }
        }

        if (this.End is not null)
        {
            if (this.EndConfidence != PeriodBoundaryConfidence.Confirmed)
            {
                return false;
            }

            if (this.End.IsApproximate)
            {
                return false;
            }

            DateOnly? earliestPossibleEnd = this.End.GetEnvelope().EarliestPossibleDate;
            if (!earliestPossibleEnd.HasValue
                || !instantEnvelope.LatestPossibleDate.HasValue
                || instantEnvelope.LatestPossibleDate.Value > earliestPossibleEnd.Value
                || (this.End.HasUncertainBoundary
                    && instantEnvelope.LatestPossibleDate.Value == earliestPossibleEnd.Value))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasDefiniteOverlap(HistoricalDateEnvelope instantEnvelope)
    {
        if (this.Ordering == HistoricalPeriodOrdering.Ambiguous
            || (this.Start is not null
                && (this.StartConfidence != PeriodBoundaryConfidence.Confirmed
                    || this.Start.IsApproximate))
            || (this.End is not null
                && (this.EndConfidence != PeriodBoundaryConfidence.Confirmed
                    || this.End.IsApproximate)))
        {
            return false;
        }

        if (this.IsPoint)
        {
            HistoricalDateEnvelope pointEnvelope = this.Start!.GetEnvelope();
            return instantEnvelope.EarliestPossibleDate.HasValue
                && instantEnvelope.LatestPossibleDate.HasValue
                && pointEnvelope.EarliestPossibleDate.HasValue
                && pointEnvelope.LatestPossibleDate.HasValue
                && instantEnvelope.EarliestPossibleDate.Value
                    <= pointEnvelope.EarliestPossibleDate.Value
                && instantEnvelope.LatestPossibleDate.Value
                    >= pointEnvelope.LatestPossibleDate.Value;
        }

        DateOnly? certainStart = this.Start?.GetEnvelope().LatestPossibleDate;
        if (this.Start is not null && !certainStart.HasValue)
        {
            return false;
        }

        DateOnly? certainEnd = this.End?.GetEnvelope().EarliestPossibleDate;
        if (this.End is not null && !certainEnd.HasValue)
        {
            return false;
        }

        if (certainStart.HasValue
            && certainEnd.HasValue
            && certainEnd.Value < certainStart.Value)
        {
            return false;
        }

        HistoricalDateEnvelope certainCoverage = new HistoricalDateEnvelope(
            certainStart,
            certainEnd,
            false);

        return certainCoverage.Overlaps(instantEnvelope);
    }

    private static HistoricalPeriodOrdering ResolveOrdering(
        HistoricalDate? start,
        HistoricalDate? end)
    {
        if (start is null || end is null)
        {
            return HistoricalPeriodOrdering.Ordered;
        }

        if (start == end)
        {
            return HistoricalPeriodOrdering.Point;
        }

        HistoricalDateEnvelope startEnvelope = start.GetEnvelope();
        HistoricalDateEnvelope endEnvelope = end.GetEnvelope();
        if (endEnvelope.IsDefinitelyBefore(startEnvelope))
        {
            throw Invalid(
                HistoricalTemporalErrorCodes.PeriodEndBeforeStart,
                "A historical period cannot certainly end before it starts.",
                nameof(end));
        }

        return startEnvelope.LatestPossibleDate.HasValue
            && endEnvelope.EarliestPossibleDate.HasValue
            && startEnvelope.LatestPossibleDate.Value <= endEnvelope.EarliestPossibleDate.Value
                ? HistoricalPeriodOrdering.Ordered
                : HistoricalPeriodOrdering.Ambiguous;
    }

    private static void ValidateConfidence(
        PeriodBoundaryConfidence confidence,
        string parameterName)
    {
        if (!Enum.IsDefined(confidence))
        {
            throw Invalid(
                HistoricalTemporalErrorCodes.InvalidBoundaryConfidence,
                "The historical period boundary confidence is invalid.",
                parameterName);
        }
    }

    private static void ValidateOpenBoundaryConfidence(
        HistoricalDate? boundary,
        PeriodBoundaryConfidence confidence,
        string parameterName)
    {
        if (boundary is null && confidence != PeriodBoundaryConfidence.Confirmed)
        {
            throw Invalid(
                HistoricalTemporalErrorCodes.OpenBoundaryConfidenceMustBeConfirmed,
                "An open historical period boundary uses Confirmed as its neutral confidence value.",
                parameterName);
        }
    }

    private static HistoricalTemporalValidationException Invalid(
        string errorCode,
        string message,
        string parameterName)
    {
        return new HistoricalTemporalValidationException(errorCode, message, parameterName);
    }
}
