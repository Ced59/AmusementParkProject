namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Compare une date de visite imprécise aux bornes historiques réellement connues.
/// </summary>
public static class RideOccurrenceHistoricalConsistencyEvaluator
{
    public static HistoricalConsistency Evaluate(
        VisitDate visitDate,
        DateOnly? openingDate,
        DateOnly? closingDate)
    {
        ArgumentNullException.ThrowIfNull(visitDate);
        if (openingDate.HasValue
            && closingDate.HasValue
            && closingDate.Value < openingDate.Value)
        {
            return HistoricalConsistency.Unverified;
        }

        DateOnly earliest = visitDate.GetEarliestPossibleDate();
        DateOnly latest = visitDate.GetLatestPossibleDate();
        if ((openingDate.HasValue && latest < openingDate.Value)
            || (closingDate.HasValue && earliest > closingDate.Value))
        {
            return HistoricalConsistency.ConfirmedConflict;
        }

        if (!openingDate.HasValue && !closingDate.HasValue)
        {
            return HistoricalConsistency.Unverified;
        }

        bool entireRangeAfterOpening = !openingDate.HasValue
            || earliest >= openingDate.Value;
        bool entireRangeBeforeClosing = !closingDate.HasValue
            || latest <= closingDate.Value;
        return entireRangeAfterOpening && entireRangeBeforeClosing
            ? HistoricalConsistency.Verified
            : HistoricalConsistency.Unverified;
    }
}
