using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Identifiant typé d'une visite, persisté et exposé sous forme de chaîne opaque.
/// </summary>
public readonly record struct VisitId
{
    private readonly string? value;

    private VisitId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized visit identifier has no value.");

    public static VisitId New()
    {
        return new VisitId(Guid.NewGuid().ToString("N"));
    }

    public static VisitId Parse(string? value)
    {
        return new VisitId(IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public static bool TryParse(string? value, out VisitId visitId)
    {
        try
        {
            visitId = Parse(value);
            return true;
        }
        catch (ArgumentException)
        {
            visitId = default;
            return false;
        }
    }

    public override string ToString()
    {
        return this.Value;
    }
}
