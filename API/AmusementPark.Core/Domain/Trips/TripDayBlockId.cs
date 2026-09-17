using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Trips;

public readonly record struct TripDayBlockId
{
    private readonly string? value;

    private TripDayBlockId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized trip day block identifier has no value.");

    public static TripDayBlockId New()
    {
        return new TripDayBlockId(Guid.NewGuid().ToString("N"));
    }

    public static TripDayBlockId Parse(string? value)
    {
        return new TripDayBlockId(IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public static bool TryParse(string? value, out TripDayBlockId id)
    {
        try
        {
            id = Parse(value);
            return true;
        }
        catch (ArgumentException)
        {
            id = default;
            return false;
        }
        catch (InvalidOperationException)
        {
            id = default;
            return false;
        }
    }

    public override string ToString()
    {
        return this.Value;
    }
}
