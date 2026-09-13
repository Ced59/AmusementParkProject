using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Sharing;

/// <summary>
/// Identifiant interne typé d'une invitation de comparaison.
/// </summary>
public readonly record struct ProfileComparisonInvitationId
{
    private readonly string? value;

    private ProfileComparisonInvitationId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized comparison invitation identifier has no value.");

    public static ProfileComparisonInvitationId New()
    {
        return new ProfileComparisonInvitationId(Guid.NewGuid().ToString("N"));
    }

    public static ProfileComparisonInvitationId Parse(string? value)
    {
        return new ProfileComparisonInvitationId(
            IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public override string ToString()
    {
        return this.Value;
    }
}
