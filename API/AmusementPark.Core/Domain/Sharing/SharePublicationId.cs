using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Sharing;

/// <summary>
/// Identifiant interne typé d'une publication, persisté sous forme de chaîne opaque.
/// </summary>
public readonly record struct SharePublicationId
{
    private readonly string? value;

    private SharePublicationId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized share publication identifier has no value.");

    public static SharePublicationId New()
    {
        return new SharePublicationId(Guid.NewGuid().ToString("N"));
    }

    public static SharePublicationId Parse(string? value)
    {
        return new SharePublicationId(IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public static bool TryParse(string? value, out SharePublicationId publicationId)
    {
        try
        {
            publicationId = Parse(value);
            return true;
        }
        catch (ArgumentException)
        {
            publicationId = default;
            return false;
        }

        catch (InvalidOperationException)
        {
            publicationId = default;
            return false;
        }
    }

    public override string ToString()
    {
        return this.Value;
    }
}
