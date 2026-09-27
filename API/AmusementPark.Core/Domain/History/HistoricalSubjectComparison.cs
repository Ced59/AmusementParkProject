namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalSubjectComparison
{
    public HistoricalSubjectComparison(
        HistoricalSubjectSnapshot from,
        HistoricalSubjectSnapshot to,
        HistoricalPresenceChange presenceChange,
        string? previousName,
        string? nextName,
        string? previousZoneId,
        string? nextZoneId,
        string? previousCategory,
        string? nextCategory)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        if (from.Subject.Type != to.Subject.Type
            || !string.Equals(from.Subject.Id, to.Subject.Id, StringComparison.Ordinal)
            || !string.Equals(
                from.Subject.ContextParkId,
                to.Subject.ContextParkId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException("A comparison requires the same historical subject at both dates.");
        }

        if (!Enum.IsDefined(presenceChange))
        {
            throw new ArgumentOutOfRangeException(nameof(presenceChange));
        }

        this.From = from;
        this.To = to;
        this.PresenceChange = presenceChange;
        this.PreviousName = Normalize(previousName);
        this.NextName = Normalize(nextName);
        this.PreviousZoneId = Normalize(previousZoneId);
        this.NextZoneId = Normalize(nextZoneId);
        this.PreviousCategory = Normalize(previousCategory);
        this.NextCategory = Normalize(nextCategory);
    }

    public HistoricalSubjectSnapshot From { get; }

    public HistoricalSubjectSnapshot To { get; }

    public HistoricalPresenceChange PresenceChange { get; }

    public string? PreviousName { get; }

    public string? NextName { get; }

    public string? PreviousZoneId { get; }

    public string? NextZoneId { get; }

    public string? PreviousCategory { get; }

    public string? NextCategory { get; }

    public bool IsRenamed => this.PreviousName is not null
        && this.NextName is not null
        && !string.Equals(this.PreviousName, this.NextName, StringComparison.Ordinal);

    public bool IsMoved => this.PreviousZoneId is not null
        && this.NextZoneId is not null
        && !string.Equals(this.PreviousZoneId, this.NextZoneId, StringComparison.Ordinal);

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
