using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Watchlists;

public readonly record struct LiveAlertSubscriptionId
{
    private readonly string? value;

    private LiveAlertSubscriptionId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized live alert subscription identifier has no value.");

    public static LiveAlertSubscriptionId New()
    {
        return new LiveAlertSubscriptionId(Guid.NewGuid().ToString("N"));
    }

    public static LiveAlertSubscriptionId Parse(string? value)
    {
        return new LiveAlertSubscriptionId(IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public static bool TryParse(string? value, out LiveAlertSubscriptionId subscriptionId)
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
    }
}
