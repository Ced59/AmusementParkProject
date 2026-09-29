using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Watchlists;

public readonly record struct LiveAlertNotificationId
{
    private readonly string? value;

    private LiveAlertNotificationId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized live alert notification identifier has no value.");

    public static LiveAlertNotificationId New()
    {
        return new LiveAlertNotificationId(Guid.NewGuid().ToString("N"));
    }

    public static LiveAlertNotificationId Parse(string? value)
    {
        return new LiveAlertNotificationId(IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public static bool TryParse(string? value, out LiveAlertNotificationId notificationId)
    {
        try
        {
            notificationId = Parse(value);
            return true;
        }
        catch (ArgumentException)
        {
            notificationId = default;
            return false;
        }
    }
}
