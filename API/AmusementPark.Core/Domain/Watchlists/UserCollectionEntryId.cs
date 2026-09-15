using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Watchlists;

/// <summary>
/// Identifiant opaque d'une intention personnelle.
/// </summary>
public readonly record struct UserCollectionEntryId
{
    private readonly string? value;

    private UserCollectionEntryId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized collection entry identifier has no value.");

    public static UserCollectionEntryId New()
    {
        return new UserCollectionEntryId(Guid.NewGuid().ToString("N"));
    }

    public static UserCollectionEntryId Parse(string? value)
    {
        return new UserCollectionEntryId(
            IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public static bool TryParse(string? value, out UserCollectionEntryId entryId)
    {
        try
        {
            entryId = Parse(value);
            return true;
        }
        catch (ArgumentException)
        {
            entryId = default;
            return false;
        }
        catch (InvalidOperationException)
        {
            entryId = default;
            return false;
        }
    }

    public override string ToString()
    {
        return this.Value;
    }
}
