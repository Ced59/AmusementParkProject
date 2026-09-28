using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.LiveData;

public readonly record struct LiveDataSourceId
{
    private readonly string? value;

    private LiveDataSourceId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized live data source identifier has no value.");

    public static LiveDataSourceId New()
    {
        return new LiveDataSourceId(Guid.NewGuid().ToString("N"));
    }

    public static LiveDataSourceId Parse(string? value)
    {
        return new LiveDataSourceId(
            IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public static bool TryParse(string? value, out LiveDataSourceId sourceId)
    {
        try
        {
            sourceId = Parse(value);
            return true;
        }
        catch (ArgumentException)
        {
            sourceId = default;
            return false;
        }
        catch (InvalidOperationException)
        {
            sourceId = default;
            return false;
        }
    }

    public override string ToString()
    {
        return this.Value;
    }
}
