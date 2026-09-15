using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Watchlists;

public readonly record struct WatchSubscriptionId
{
    private readonly string? value;

    private WatchSubscriptionId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized watch subscription identifier has no value.");

    public static WatchSubscriptionId New()
    {
        return new WatchSubscriptionId(Guid.NewGuid().ToString("N"));
    }

    public static WatchSubscriptionId Parse(string? value)
    {
        return new WatchSubscriptionId(
            IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public static bool TryParse(string? value, out WatchSubscriptionId subscriptionId)
    {
        try
        {
            subscriptionId = Parse(value);
            return true;
        }
        catch (ArgumentException)
        {
            subscriptionId = default;
            return false;
        }
        catch (InvalidOperationException)
        {
            subscriptionId = default;
            return false;
        }
    }

    public override string ToString()
    {
        return this.Value;
    }
}
