using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Identifiant typé d'une occurrence de ride, persisté et exposé sous forme de chaîne opaque.
/// </summary>
public readonly record struct RideOccurrenceId
{
    private readonly string? value;

    private RideOccurrenceId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized ride occurrence identifier has no value.");

    public static RideOccurrenceId New()
    {
        return new RideOccurrenceId(Guid.NewGuid().ToString("N"));
    }

    public static RideOccurrenceId Parse(string? value)
    {
        return new RideOccurrenceId(IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public override string ToString()
    {
        return this.Value;
    }
}
