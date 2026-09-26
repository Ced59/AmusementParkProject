namespace AmusementPark.Core.Domain.History;

public static class HistoricalTimelineOrdering
{
    public static int ResolveDayNumber(HistoricalPeriod period)
    {
        ArgumentNullException.ThrowIfNull(period);
        DateOnly date = period.Start?.GetEnvelope().EarliestPossibleDate
            ?? period.End?.GetEnvelope().EarliestPossibleDate
            ?? DateOnly.MinValue;
        return date.DayNumber;
    }
}
