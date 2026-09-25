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
        HistoricalDateEnvelope? guaranteedCoverage = this.GetGuaranteedCoverageEnvelope();
        return guaranteedCoverage is not null
            && guaranteedCoverage.Contains(instantEnvelope);
    }

    private bool HasDefiniteOverlap(HistoricalDateEnvelope instantEnvelope)
    {
        if (this.Ordering == HistoricalPeriodOrdering.Ambiguous)
        {
            return false;
        }

        HistoricalDateEnvelope? guaranteedCoverage = this.GetGuaranteedCoverageEnvelope();
        if (guaranteedCoverage is not null && guaranteedCoverage.Overlaps(instantEnvelope))
        {
            return true;
        }

        if (IsUsableBoundary(this.Start, this.StartConfidence)
            && instantEnvelope.Contains(this.Start!.GetEnvelope()))
        {
            return true;
        }

        return IsUsableBoundary(this.End, this.EndConfidence)
            && instantEnvelope.Contains(this.End!.GetEnvelope());
    }

    private HistoricalDateEnvelope? GetGuaranteedCoverageEnvelope()
    {
        if (this.Ordering == HistoricalPeriodOrdering.Ambiguous)
        {
            return null;
        }

        if (this.IsPoint)
        {
            HistoricalDateEnvelope pointEnvelope = this.Start!.GetEnvelope();
            return IsUsableBoundary(this.Start, this.StartConfidence)
                && pointEnvelope.IsExactDay
                    ? pointEnvelope
                    : null;
        }

        bool hasUsableStart = this.Start is null
            || (IsUsableBoundary(this.Start, this.StartConfidence)
                && this.Start.GetEnvelope().LatestPossibleDate.HasValue);
        bool hasUsableEnd = this.End is null
            || (IsUsableBoundary(this.End, this.EndConfidence)
                && this.End.GetEnvelope().EarliestPossibleDate.HasValue);

        if (hasUsableStart && hasUsableEnd)
        {
            DateOnly? certainStart = this.Start?.GetEnvelope().LatestPossibleDate;
            DateOnly? certainEnd = this.End?.GetEnvelope().EarliestPossibleDate;

            if (certainStart.HasValue
                && certainEnd.HasValue
                && certainEnd.Value < certainStart.Value)
            {
                return null;
            }

            return new HistoricalDateEnvelope(certainStart, certainEnd, false);
        }

        if (hasUsableStart && this.Start is not null)
        {
            HistoricalDateEnvelope startEnvelope = this.Start.GetEnvelope();
            if (startEnvelope.IsExactDay)
            {
                return startEnvelope;
            }
        }

        if (hasUsableEnd && this.End is not null)
        {
            HistoricalDateEnvelope endEnvelope = this.End.GetEnvelope();
            if (endEnvelope.IsExactDay)
            {
                return endEnvelope;
            }
        }

        return null;
    }

    private static bool IsUsableBoundary(
        HistoricalDate? boundary,
        PeriodBoundaryConfidence confidence)
    {
        return boundary is not null
            && confidence == PeriodBoundaryConfidence.Confirmed
            && !boundary.IsApproximate;
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
