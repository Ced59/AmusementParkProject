using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Trips;

public readonly record struct TripDayPlanId
{
    private readonly string? value;

    private TripDayPlanId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized trip day plan identifier has no value.");

    public static TripDayPlanId New()
    {
        return new TripDayPlanId(Guid.NewGuid().ToString("N"));
    }

    public static TripDayPlanId Parse(string? value)
    {
        return new TripDayPlanId(IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public override string ToString()
    {
        return this.Value;
    }
}
