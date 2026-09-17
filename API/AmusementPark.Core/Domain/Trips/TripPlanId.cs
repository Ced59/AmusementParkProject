using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Trips;

public readonly record struct TripPlanId
{
    private readonly string? value;

    private TripPlanId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized trip plan identifier has no value.");

    public static TripPlanId New()
    {
        return new TripPlanId(Guid.NewGuid().ToString("N"));
    }

    public static TripPlanId Parse(string? value)
    {
        return new TripPlanId(IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public static bool TryParse(string? value, out TripPlanId tripPlanId)
    {
        try
        {
            tripPlanId = Parse(value);
            return true;
        }
        catch (ArgumentException)
        {
            tripPlanId = default;
            return false;
        }
        catch (InvalidOperationException)
        {
            tripPlanId = default;
            return false;
        }
    }

    public override string ToString()
    {
        return this.Value;
    }
}
