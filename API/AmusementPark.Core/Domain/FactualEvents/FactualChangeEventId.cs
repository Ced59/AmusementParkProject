using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.FactualEvents;

public readonly record struct FactualChangeEventId
{
    private readonly string? value;

    private FactualChangeEventId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized factual event identifier has no value.");

    public static FactualChangeEventId New()
    {
        return new FactualChangeEventId(Guid.NewGuid().ToString("N"));
    }

    public static FactualChangeEventId Parse(string? value)
    {
        return new FactualChangeEventId(
            IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public static bool TryParse(string? value, out FactualChangeEventId eventId)
    {
        try
        {
            eventId = Parse(value);
            return true;
        }
        catch (ArgumentException)
        {
            eventId = default;
            return false;
        }
        catch (InvalidOperationException)
        {
            eventId = default;
            return false;
        }
    }

    public override string ToString()
    {
        return this.Value;
    }
}
