namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalPresenceInterval
{
    public HistoricalPresenceInterval(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new ArgumentOutOfRangeException(nameof(end));
        }

        this.Start = start;
        this.End = end;
    }

    public DateOnly Start { get; }

    public DateOnly End { get; }
}
