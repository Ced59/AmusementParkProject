using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.ParkFit;

public readonly record struct ParkFitGroupProfileId
{
    private readonly string? value;

    private ParkFitGroupProfileId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized Park Fit group profile identifier has no value.");

    public static ParkFitGroupProfileId New()
    {
        return new ParkFitGroupProfileId(Guid.NewGuid().ToString("N"));
    }

    public static ParkFitGroupProfileId Parse(string? value)
    {
        return new ParkFitGroupProfileId(
            IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public static bool TryParse(string? value, out ParkFitGroupProfileId profileId)
    {
        try
        {
            profileId = Parse(value);
            return true;
        }
        catch (ArgumentException)
        {
            profileId = default;
            return false;
        }
        catch (InvalidOperationException)
        {
            profileId = default;
            return false;
        }
    }

    public override string ToString()
    {
        return this.Value;
    }
}
