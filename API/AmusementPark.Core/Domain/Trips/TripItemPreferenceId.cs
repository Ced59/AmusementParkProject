using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Trips;

public readonly record struct TripItemPreferenceId
{
    private TripItemPreferenceId(string value)
    {
        this.Value = value;
    }

    public string Value { get; }

    public static TripItemPreferenceId New()
    {
        return new TripItemPreferenceId(Guid.NewGuid().ToString("D"));
    }

    public static TripItemPreferenceId Parse(string value)
    {
        return new TripItemPreferenceId(IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public static bool TryParse(string? value, out TripItemPreferenceId preferenceId)
    {
        try
        {
            preferenceId = Parse(value ?? string.Empty);
            return true;
        }
        catch (ArgumentException)
        {
            preferenceId = default;
            return false;
        }
    }

    public override string ToString()
    {
        return this.Value;
    }
}
