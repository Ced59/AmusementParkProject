using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Sharing;

/// <summary>
/// Identifiant interne typé de l'accord de comparaison créé après les deux consentements.
/// </summary>
public readonly record struct ProfileComparisonId
{
    private readonly string? value;

    private ProfileComparisonId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized profile comparison identifier has no value.");

    public static ProfileComparisonId New()
    {
        return new ProfileComparisonId(Guid.NewGuid().ToString("N"));
    }

    public static ProfileComparisonId Parse(string? value)
    {
        return new ProfileComparisonId(
            IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public override string ToString()
    {
        return this.Value;
    }
}
