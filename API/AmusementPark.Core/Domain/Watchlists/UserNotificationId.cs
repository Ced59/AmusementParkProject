using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Watchlists;

public readonly record struct UserNotificationId
{
    private UserNotificationId(string value)
    {
        this.Value = IdentifierRules.NormalizeRequired(value, nameof(value));
    }

    public string Value { get; }

    public static UserNotificationId New()
    {
        return new UserNotificationId(Guid.NewGuid().ToString("N"));
    }

    public static UserNotificationId Parse(string? value)
    {
        return new UserNotificationId(value ?? string.Empty);
    }

    public static bool TryParse(string? value, out UserNotificationId notificationId)
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

    public override string ToString()
    {
        return this.Value;
    }
}
