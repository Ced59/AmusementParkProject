namespace AmusementPark.Core.Domain.FactualEvents;

/// <summary>
/// Structured before/after difference for one factual field.
/// </summary>
public sealed record FactualChangeDiff
{
    private FactualChangeDiff(FactValue? previousValue, FactValue? newValue)
    {
        this.PreviousValue = previousValue;
        this.NewValue = newValue;
    }

    public FactValue? PreviousValue { get; }

    public FactValue? NewValue { get; }

    public static FactualChangeDiff? Detect(FactValue? previousValue, FactValue? newValue)
    {
        if (previousValue == newValue)
        {
            return null;
        }

        return new FactualChangeDiff(previousValue, newValue);
    }
}
