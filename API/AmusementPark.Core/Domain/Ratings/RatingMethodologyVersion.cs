using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Identifiant immuable de la méthodologie appliquée aux notes communautaires.
/// </summary>
public readonly record struct RatingMethodologyVersion
{
    private readonly string? value;

    private RatingMethodologyVersion(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized rating methodology version has no value.");

    public static RatingMethodologyVersion Parse(string? value)
    {
        return new RatingMethodologyVersion(IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public override string ToString()
    {
        return this.Value;
    }
}
