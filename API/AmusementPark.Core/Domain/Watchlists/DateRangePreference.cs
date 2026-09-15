namespace AmusementPark.Core.Domain.Watchlists;

/// <summary>
/// Période privée envisagée sans imposer que ses deux bornes soient déjà connues.
/// </summary>
public sealed record DateRangePreference
{
    public DateRangePreference(DateOnly? startsOn, DateOnly? endsOn)
    {
        if (!startsOn.HasValue && !endsOn.HasValue)
        {
            throw InvalidPeriod("A preferred period requires at least one date boundary.");
        }

        if (startsOn.HasValue && endsOn.HasValue && endsOn.Value < startsOn.Value)
        {
            throw InvalidPeriod("A preferred period cannot end before it starts.");
        }

        this.StartsOn = startsOn;
        this.EndsOn = endsOn;
    }

    public DateOnly? StartsOn { get; }

    public DateOnly? EndsOn { get; }

    public bool Contains(DateOnly date)
    {
        return (!this.StartsOn.HasValue || date >= this.StartsOn.Value)
            && (!this.EndsOn.HasValue || date <= this.EndsOn.Value);
    }

    private static UserCollectionValidationException InvalidPeriod(string message)
    {
        return new UserCollectionValidationException(
            UserCollectionErrorCodes.InvalidPreferredPeriod,
            message);
    }
}
